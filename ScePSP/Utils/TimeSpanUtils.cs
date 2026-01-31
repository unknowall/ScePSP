using System;
using System.Timers;

namespace ScePSPUtils
{
    public static class TimeSpanUtils
    {
        public static TimeSpan FromMicroseconds(long microseconds)
        {
            long ticks = microseconds * 10;

            return TimeSpan.FromTicks(ticks);
        }

        public static void InfiniteLoopDetector(string description, Action action, Action loopAction = null)
        {
            using (var timer = new Timer(4.0 * 1000))
            {
                bool[] cancel = { false };
                timer.Elapsed += (sender, e) =>
                {
                    if (cancel[0]) return;
                    Console.WriteLine("InfiniteLoop Detected! : {0} : {1}", description, e.SignalTime);
                    loopAction?.Invoke();
                };
                timer.AutoReset = false;
                timer.Start();
                try
                {
                    action();
                }
                finally
                {
                    cancel[0] = true;
                    timer.Enabled = false;
                    timer.Stop();
                }
            }
        }
    }
}