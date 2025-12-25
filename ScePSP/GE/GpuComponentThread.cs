using ScePSP.Core.Gpu;
using ScePSP.Utils;
using System.Threading;
using ScePSP.Core.Components.Display;

namespace ScePSP.Runner.Components.Gpu
{
    public sealed class GpuComponentThread : ComponentThread
    {
        protected override string ThreadName => "CpuThread";

        [Inject] private GpuProcessor GpuProcessor;

        [Inject] private GpuImpl GpuImpl;

        [Inject] private DisplayConfig DisplayConfig;

        protected override void Main()
        {
            GpuImpl.InitSynchronizedOnce(DisplayConfig.WindowHandle);

            GpuProcessor.ProcessInit();

            //Console.WriteLine("GpuComponentThread.Start()");
            try
            {
                while (true)
                {
                    WaitHandle.WaitAny(new WaitHandle[] { GpuProcessor.DisplayListQueueUpdated, ThreadTaskQueue.EnqueuedEvent, RunningUpdatedEvent }, 200.Milliseconds());

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
                //Console.WriteLine("GpuComponentThread.End()");
            }
        }
    }
}