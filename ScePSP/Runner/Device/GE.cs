using ScePSP.GE;
using System.Threading;

namespace ScePSP.Runner.GE
{
    public sealed class GEThread : DeviceThread
    {
        protected override string ThreadName { get { return "GEThread"; } }

        [Context]
        private GEList GeList;

        [Context]
        private GEBackEnd BackEnd;

        [Context]
        private GEConfig Config;

        protected override void Main()
        {
            BackEnd.InitSynchronizedOnce(Config.WindowHandle);

            GeList.ProcessInit();

            while (true)
            {
                var Ret = WaitHandle.WaitAny(new WaitHandle[] { GeList.QueueEvent, ThreadTaskQueue.EnqueuedEvent, RunningUpdatedEvent }, 200);
                if (Ret == WaitHandle.WaitTimeout) continue;

                ThreadTaskQueue.HandleEnqueued();
                if (!Running) return;

                GeList.SetCurrent();
                GeList.ProcessStep();
                GeList.UnsetCurrent();
            }
        }
    }
}
