using ScePSP.Core.Components.Display;
using ScePSP.Core.GpuBackEnd;
using ScePSP.Utils;
using System;
using System.Threading;

namespace ScePSP.Runner.Tasks.Gpu
{
    public sealed class GpuTask : PspDeviceTask
    {
        protected override string ThreadName => "GpuTask";

        private GpuProcessor GpuProcessor => PSPDrivers.GE;

        private GpuBackEnd GpuBackEnd => PSPDrivers.GpuBackEnd;

        private DisplayConfig DisplayConfig => PSPDrivers.Config.DisplayConfig;

        protected override void Main()
        {
            var threadId = Environment.CurrentManagedThreadId;
            Console.Out.WriteLineColored(ConsoleColor.White, $"## GE Runing ThreadId={threadId}");

            GpuBackEnd.InitSynchronizedOnce(DisplayConfig.WindowHandle);

            GpuProcessor.ProcessInit();

            try
            {
                while (true)
                {
                    WaitHandle.WaitAny(new WaitHandle[] { GpuProcessor.GEProcessQueueUpdated, ThreadTaskQueue.EnqueuedEvent, RunningUpdatedEvent }, 200.Milliseconds());

                    // TODO: Should wait until the Form has created its context.

                    ThreadTaskQueue.HandleEnqueued();

                    if (!Running) break;

                    GpuProcessor.SetCurrent();
                    GpuProcessor.ProcessStep();
                    GpuProcessor.UnsetCurrent();
                }
            }
            finally
            {
                //Console.WriteLine("GpuTask.End()");
            }
        }
    }
}