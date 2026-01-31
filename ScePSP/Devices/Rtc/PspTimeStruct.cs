using LightGL.DynamicLibrary;
using ScePSPUtils;
using System;

namespace ScePSP.Rtc
{
    // Token: 0x0200005D RID: 93
    public struct PspTimeStruct
    {
        // Token: 0x06000191 RID: 401 RVA: 0x00046CAF File Offset: 0x00044EAF
        public void SetToDateTime(DateTime DateTime)
        {
            this.TotalMicroseconds = Platform.CurrentUnixMicroseconds;
        }

        // Token: 0x06000192 RID: 402 RVA: 0x00068B74 File Offset: 0x00066D74
        public void SetToNow()
        {
            long totalMicroseconds = this.TotalMicroseconds;
            long currentUnixMicroseconds = Platform.CurrentUnixMicroseconds;
            if (currentUnixMicroseconds < totalMicroseconds)
            {
                PspTimeStruct.Logger.Error("Total Microseconds overflow Prev({0}), Now({1})", new object[]
                {
                    totalMicroseconds,
                    currentUnixMicroseconds
                });
            }
            this.TotalMicroseconds = currentUnixMicroseconds;
        }

        // Token: 0x040001D8 RID: 472
        public static Logger Logger = Logger.GetLogger("Rtc");

        // Token: 0x040001D9 RID: 473
        public long TotalMicroseconds;
    }
}
