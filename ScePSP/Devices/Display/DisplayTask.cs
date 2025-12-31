using ScePSP.Core.Components.Display;
using ScePSP.Hle.Managers;
using ScePSP.Utils;
using ScePSPUtils;
using System;
using System.Threading;

namespace ScePSP.Runner.Tasks.Display
{
    public sealed class DisplayTask : PspDeviceTask
    {
        private HleInterruptManager _hleInterruptManager;

        private PspDisplay _pspDisplay;

        public DisplayTask(HleInterruptManager hleInterruptManager, PspDisplay pspDisplay)
        {
            _hleInterruptManager = hleInterruptManager;
            _pspDisplay = pspDisplay;
        }

        protected override string ThreadName => "DisplayTask";

        TimeSpan vSyncTimeIncrement = TimeSpan.FromSeconds(1.0 / (PspDisplay.HorizontalSyncHertz / (double)PspDisplay.VsyncRow));

        //var VSyncTimeIncrement = TimeSpan.FromSeconds(1.0 / (PspDisplay.HorizontalSyncHertz / (double)(PspDisplay.VsyncRow / 2))); // HACK to give more time to render!

        TimeSpan endTimeIncrement = TimeSpan.FromSeconds(1.0 / (PspDisplay.HorizontalSyncHertz / (double)PspDisplay.NumberOfRows));

        HleInterruptHandler vBlankInterruptHandler;

        public bool triggerStuff = true;

        public void Step(Action DrawStart, Action VBlankStart, Action VBlankEnd)
        {
            var startTime = DateTime.UtcNow;
            var vSyncTime = startTime + vSyncTimeIncrement;
            var endTime = startTime + endTimeIncrement;

            ThreadTaskQueue.HandleEnqueued();

            if (!Running) return;

            // Draw time
            DrawStart();
            ThreadUtils.SleepUntilUtc(vSyncTime);

            // VBlank time
            VBlankStart();
            vBlankInterruptHandler.Trigger();
            ThreadUtils.SleepUntilUtc(endTime);
            VBlankEnd();
        }

        protected override void Main()
        {
            var threadId = Environment.CurrentManagedThreadId;
            Console.Out.WriteLineColored(ConsoleColor.White, $"## DISPLAY Runing ThreadId={threadId}");

            vBlankInterruptHandler = _hleInterruptManager.GetInterruptHandler(PspInterrupts.PspVblankInt);

            try
            {
                while (Running)
                {
                    if (triggerStuff)
                    {
                        Step(_pspDisplay.TriggerDrawStart, _pspDisplay.TriggerVBlankStart, _pspDisplay.TriggerVBlankEnd);
                    }
                    else
                    {
                        Thread.Sleep(16.Milliseconds());
                    }
                }
            }
            finally
            {
                //Console.WriteLine("DisplayTask.End()");
            }
        }
    }
}