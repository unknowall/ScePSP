using ScePSP.Audio;
using System.Threading;

namespace ScePSP.Runner.Audio
{
    public sealed class AudioThread : DeviceThread
    {
        [Context]
        private PspAudio PspAudio;

        protected override string ThreadName { get { return "AudioThread"; } }

        protected override void Main()
        {
            Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
            //Thread.CurrentThread.Priority = ThreadPriority.Normal;
            while (true)
            {
                ThreadTaskQueue.HandleEnqueued();
                if (!Running) break;

                PspAudio.Update();
                Thread.Sleep(1);
            }
            PspAudio.StopSynchronized();
        }
    }
}
