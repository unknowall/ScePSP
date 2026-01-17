using ScePSP.Devices.Display;
using ScePSP.BackEnd;
using ScePSP.GE;
using ScePSP.Utils;
using System;
using System.Threading;

namespace ScePSP.Runner.Tasks.GE
{
    public sealed class GETask : PspMainTask
    {
        protected override string ThreadName => "GpuTask";

        private GEList GEList => PSPDrivers.GEList;

        private GEBackEnd GpuBackEnd => PSPDrivers.GeBackEnd;

        private DisplayConfig DisplayConfig => PSPDrivers.Config.DisplayConfig;

        protected override void Main()
        {
            var threadId = Environment.CurrentManagedThreadId;

            Console.Out.WriteLineColored(ConsoleColor.White, $"## GE Runing ThreadId={threadId}");

            GpuBackEnd.InitSynchronizedOnce(DisplayConfig.WindowHandle);

            GEList.ProcessInit();

            while (Running)
            {
                WaitHandle.WaitAny(new WaitHandle[] { GEList.QueueEvent, ThreadTaskQueue.EnqueuedEvent, RunningUpdatedEvent }, 200.Milliseconds());

                ThreadTaskQueue.HandleEnqueued();

                if (!Running) break;

                GEList.SetCurrent();
                GEList.ProcessStep();
                GEList.UnsetCurrent();
            }
        }
    }
}