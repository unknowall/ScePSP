using ScePSP.Core.Audio;
using System.Threading;

namespace ScePSP.Runner.Tasks.Audio
{
    public sealed class AudioTask : PspDeviceTask
    {
        [Inject] private PspAudio PspAudio;

        protected override string ThreadName => "AudioTask";

        protected override void Main()
        {
            //Console.WriteLine("AudioTask.Start()");
            try
            {
                while (true)
                {
                    ThreadTaskQueue.HandleEnqueued();
                    if (!Running) break;

                    PspAudio.Update();
                    Thread.Sleep(1);
                }

                PspAudio.StopSynchronized();
            }
            finally
            {
                //Console.WriteLine("AudioTask.End()");
            }
        }
    }
}