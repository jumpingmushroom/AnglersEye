namespace AnglersEye.Core.Model
{
    /// <summary>
    /// The length of the hooked fish's current struggle, for the panel's draining bar. A struggle
    /// starts when escaping turns on, or when its timer jumps back up while escaping (a new roll).
    /// Fed once per frame.
    /// </summary>
    public sealed class StruggleTracker
    {
        private bool _wasEscaping;
        private float _lastRemaining;

        /// <summary>The current (or last) struggle's total; 0 when nothing is hooked.</summary>
        public float Total { get; private set; }

        public float Update(bool hasCatch, bool escaping, float remaining)
        {
            if (!hasCatch)
            {
                Total = 0f;
                _wasEscaping = false;
                _lastRemaining = 0f;
                return Total;
            }
            if (escaping && (!_wasEscaping || remaining > _lastRemaining))
                Total = remaining;
            _wasEscaping = escaping;
            _lastRemaining = remaining;
            return Total;
        }
    }
}
