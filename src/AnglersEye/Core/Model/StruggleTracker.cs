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
        private float _total;

        /// <summary>Feed one frame; returns the current (or last) struggle's total, 0 when nothing is hooked.</summary>
        public float Update(bool hasCatch, bool escaping, float remaining)
        {
            if (!hasCatch)
            {
                _total = 0f;
                _wasEscaping = false;
                _lastRemaining = 0f;
                return _total;
            }
            if (escaping && (!_wasEscaping || remaining > _lastRemaining))
                _total = remaining;
            _wasEscaping = escaping;
            _lastRemaining = remaining;
            return _total;
        }
    }
}
