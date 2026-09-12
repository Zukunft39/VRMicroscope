namespace VRMicroscope.Assistant
{
    // Pure timing policy: suspended time and reading time never accumulate as inactivity.
    public sealed class AssistantIdleClock
    {
        public float Elapsed { get; private set; }
        public int ReminderCount { get; private set; }
        public void Activity() { Elapsed = 0; ReminderCount = 0; }
        public bool Tick(float seconds, bool suppressed, float threshold, int limit)
        {
            if (suppressed) { Elapsed = 0; return false; }
            if (seconds <= 0 || limit > 0 && ReminderCount >= limit) return false;
            Elapsed += seconds;
            if (Elapsed < threshold) return false;
            Elapsed = 0;
            ReminderCount++;
            return true;
        }
    }
}
