namespace DNPDemo.Audio
{
    /// <summary>
    /// Keeps a sound effect from playing over a voice line (<see cref="SePlayer"/>). A request while a line plays is held
    /// and released once the line has ended. Only the latest request is held: a newer one replaces it. A request that has
    /// waited <see cref="MaxWaitSeconds"/> is released even though the line goes on, so a long line never swallows the
    /// sound of an operation; it plays lowered then (<see cref="SeDucking"/>).
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
        /// The held event, once no line plays any more or it has waited <see cref="MaxWaitSeconds"/>. Returns false while
        /// it still waits or when nothing is held.
        /// </summary>
        public bool TryRelease(float now, bool linePlaying, out SeEvent seEvent)
        {
            seEvent = default;
            if (!_held || (linePlaying && now - _requestedAt < MaxWaitSeconds))
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
