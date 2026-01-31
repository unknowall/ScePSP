using ScePSPUtils;
using System;
using System.Collections.Generic;

namespace ScePSP.Rtc
{
    public class PspRtc
    {
        public DateTime StartDateTime { get; protected set; }

        public DateTime CurrentDateTime { get; protected set; }

        public PspTimeStruct ElapsedTime
        {
            get
            {
                return this._ElapsedTime;
            }
        }

        public TimeSpan Elapsed
        {
            get
            {
                return this.CurrentDateTime - this.StartDateTime;
            }
        }

        public uint UnixTimeStamp
        {
            get
            {
                return (uint)(this.CurrentDateTime - new DateTime(1970, 1, 1)).TotalSeconds;
            }
        }

        public PspRtc()
        {
            this.Start();
        }

        public void Start()
        {
            this.StartDateTime = DateTime.UtcNow;
            this.StartTime.SetToNow();
        }

        protected virtual void UpdateInternal()
        {
            this.CurrentTime.SetToNow();
            this.CurrentDateTime = DateTime.UtcNow;
        }

        public void Update()
        {
            this.UpdateInternal();
            this._ElapsedTime.TotalMicroseconds = this.CurrentTime.TotalMicroseconds - this.StartTime.TotalMicroseconds;
            lock (this.Timers)
            {
                for (; ; )
                {
                IL_39:
                    foreach (PspVirtualTimer pspVirtualTimer in this.Timers)
                    {
                        lock (pspVirtualTimer)
                        {
                            if (pspVirtualTimer.Enabled && this.CurrentDateTime >= pspVirtualTimer.DateTime)
                            {
                                this.Timers.Remove(pspVirtualTimer);
                                pspVirtualTimer.Callback();
                                pspVirtualTimer.OnList = false;
                                goto IL_39;
                            }
                        }
                    }
                    break;
                }
            }
        }

        public PspVirtualTimer CreateVirtualTimer(Action Callback)
        {
            return new PspVirtualTimer(this)
            {
                Callback = Callback
            };
        }

        public PspVirtualTimer RegisterTimerInOnce(TimeSpan TimeSpan, Action Callback)
        {
            PspRtc.Logger.Notice("RegisterTimerInOnce: " + TimeSpan, new object[0]);
            this.Update();
            return this.RegisterTimerAtOnce(this.CurrentDateTime + TimeSpan, Callback);
        }

        public PspVirtualTimer RegisterTimerAtOnce(DateTime DateTime, Action Callback)
        {
            PspVirtualTimer result;
            lock (this.Timers)
            {
                PspRtc.Logger.Notice("RegisterTimerAtOnce: " + DateTime, new object[0]);
                PspVirtualTimer pspVirtualTimer = this.CreateVirtualTimer(Callback);
                pspVirtualTimer.SetAt(DateTime);
                pspVirtualTimer.Enabled = true;
                result = pspVirtualTimer;
            }
            return result;
        }

        public unsafe void RegisterTimeout(uint* Timeout, Action WakeUpCallback)
        {
            if (Timeout != null)
            {
                this.RegisterTimerInOnce(TimeSpanUtils.FromMicroseconds((long)((ulong)(*Timeout))), delegate
                {
                    WakeUpCallback();
                });
            }
        }

        public static Logger Logger = Logger.GetLogger("Rtc");

        internal LinkedList<PspVirtualTimer> Timers = new LinkedList<PspVirtualTimer>();

        protected PspTimeStruct StartTime;

        protected PspTimeStruct CurrentTime;

        protected PspTimeStruct _ElapsedTime;
    }
}
