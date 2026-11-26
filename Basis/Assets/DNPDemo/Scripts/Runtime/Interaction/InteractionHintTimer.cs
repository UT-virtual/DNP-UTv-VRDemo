namespace DNPDemo.Interaction
{
    /// <summary>
    /// Decides when the hint of the running part appears: <see cref="InteractionHint.DelaySeconds"/> after
    /// <see cref="Start"/>. Times are passed in, so it can be tested without a running clock.
    /// </summary>
    public sealed class InteractionHintTimer
    {
        float _dueTime;
        bool _running;

        /// <summary>The delay has passed and the hint is out. Cleared by <see cref="Start"/> and <see cref="Clear"/>.</summary>
        public bool IsShown { get; private set; }

        /// <summary>Starts counting for a part that began at <paramref name="now"/>. A disabled or missing hint never appears.</summary>
        public void Start(InteractionHint hint, float now)
        {
            IsShown = false;
            _running = hint != null && hint.IsEnabled;
            _dueTime = _running ? now + hint.DelaySeconds : 0f;
        }

        /// <summary>No part is running: nothing appears until the next <see cref="Start"/>.</summary>
        public void Clear()
        {
            _running = false;
            IsShown = false;
        }

        /// <summary>Returns true once, on the first call at or after the due time; the hint should appear then.</summary>
        public bool Tick(float now)
        {
            if (!_running || IsShown || now < _dueTime)
            {
                return false;
            }
            IsShown = true;
            return true;
        }
    }
}
