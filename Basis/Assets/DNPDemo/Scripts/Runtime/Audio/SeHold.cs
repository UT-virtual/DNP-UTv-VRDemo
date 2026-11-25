namespace DNPDemo.Audio
{
    /// <summary>
    /// Keeps a sound effect from playing over a voice line (<see cref="SePlayer"/>). A request while a line plays is held
    /// and released once the line has ended. Only the latest request is held: a newer one replaces it. A held request that
    /// is older than <see cref="MaxWaitSeconds"/> is dropped, because by then the operation it answered is long past.
    /// Times are in seconds on one clock (the player uses unscaled time).
    /// </summary>
    internal sealed class SeHold
    {
        public const float MaxWaitSeconds = 2f;

        bool _held;
        SeEvent _event;
        float _requestedAt;

        public bool IsHolding => _held;

        /// <summary>True when <paramref name="seEvent"/> can play now; otherwise it is held (replacing any held one).</summary>
        public bool Request(SeEvent seEvent, float now, bool linePlaying)
        {
            if (!linePlaying)
            {
                _held = false;
                return true;
            }
            _held = true;
            _event = seEvent;
            _requestedAt = now;
            return false;
        }

        /// <summary>
        /// The held event, once no line plays any more. Returns false while a line still plays, when nothing is held, or
        /// when the held event has waited too long (it is dropped then).
        /// </summary>
        public bool TryRelease(float now, bool linePlaying, out SeEvent seEvent)
        {
            seEvent = default;
            if (!_held)
            {
                return false;
            }
            if (now - _requestedAt > MaxWaitSeconds)
            {
                _held = false;
                return false;
            }
            if (linePlaying)
            {
                return false;
            }
            _held = false;
            seEvent = _event;
            return true;
        }

        public void Clear() => _held = false;
    }
}
