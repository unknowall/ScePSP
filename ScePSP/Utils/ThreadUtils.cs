using System;
using System.Threading;

namespace ScePSPUtils
{
    public class ThreadUtils
    {
        public static void SleepUntilUtc(DateTime until)
        {
            var duration = until - DateTime.UtcNow;
            if (duration.TotalSeconds < 0) return;
            Thread.Sleep(duration);
        }
    }
}