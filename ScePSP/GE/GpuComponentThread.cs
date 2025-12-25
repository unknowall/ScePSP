using ScePSP.Core;
using ScePSP.Core.Components.Display;
using ScePSP.Core.Gpu;
using ScePSP.Core.Gpu.Impl.Opengl;
using ScePSP.Utils;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

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

                    //var OpenglImpl = (GpuImpl as OpenglGpuImpl);
                    //if (OpenglImpl.RenderbufferManager.CurrentDrawBuffer != null)
                    //{
                    //    byte[] ColorPixels = OpenglImpl.RenderbufferManager.CurrentDrawBuffer.RenderTarget.ReadPixels();
                    //    Console.WriteLine($"{OpenglGpuImpl.RenderbufferManager.CurrentDrawBuffer.RenderTarget.ToString()} ColorPixels {ColorPixels.Length}");
                    //    File.WriteAllBytes(ApplicationPaths.AssertPath + "/draw.bin", ColorPixels);
                    //}

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