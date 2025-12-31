using ScePSP.Core.Components.Display;
using ScePSP.Core.Gpu;
using ScePSP.Utils;
using System;
using System.Threading;

namespace ScePSP.Runner.Tasks.Gpu
{
    public sealed class GpuTask : PspDeviceTask
    {
        protected override string ThreadName => "GpuTask";

        [Inject] private GpuProcessor GpuProcessor;

        [Inject] private GpuImpl GpuImpl;

        [Inject] private DisplayConfig DisplayConfig;

        protected override void Main()
        {
            var threadId = Environment.CurrentManagedThreadId;
            Console.Out.WriteLineColored(ConsoleColor.White, $"## GE Runing ThreadId={threadId}");

            GpuImpl.InitSynchronizedOnce(DisplayConfig.WindowHandle);

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