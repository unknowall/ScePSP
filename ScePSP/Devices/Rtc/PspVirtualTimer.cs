using System;

namespace ScePSP.Rtc
{
    // Token: 0x0200005E RID: 94
    public class PspVirtualTimer
    {
        // Token: 0x1700002C RID: 44
        // (get) Token: 0x06000195 RID: 405 RVA: 0x00046CCD File Offset: 0x00044ECD
        // (set) Token: 0x06000194 RID: 404 RVA: 0x00068BC8 File Offset: 0x00066DC8
        public DateTime DateTime
        {
            get
            {
                return this._DateTime;
            }
            set
            {
                lock (this.PspRtc.Timers)
                {
                    lock (this)
                    {
                        this._DateTime = value;
                        if (!this.OnList)
                        {
                            this.PspRtc.Timers.AddLast(this);
                            this.OnList = true;
                        }
                    }
                }
            }
        }

        // Token: 0x06000196 RID: 406 RVA: 0x00046CD5 File Offset: 0x00044ED5
        internal PspVirtualTimer(PspRtc PspRtc)
        {
            this.PspRtc = PspRtc;
        }

        // Token: 0x06000197 RID: 407 RVA: 0x00046CE4 File Offset: 0x00044EE4
        public void SetIn(TimeSpan TimeSpan)
        {
            this.DateTime = DateTime.UtcNow + TimeSpan;
        }

        // Token: 0x06000198 RID: 408 RVA: 0x00046CF7 File Offset: 0x00044EF7
        public void SetAt(DateTime DateTime)
        {
            this.DateTime = DateTime;
        }

        // Token: 0x040001DA RID: 474
        protected PspRtc PspRtc;

        // Token: 0x040001DB RID: 475
        protected DateTime _DateTime;

        // Token: 0x040001DC RID: 476
        public bool OnList;

        // Token: 0x040001DD RID: 477
        internal Action Callback;

        // Token: 0x040001DE RID: 478
        public bool Enabled;
    }
}
