using ScePSP.Components.Display;
using ScePSP.GE.State;
using ScePSP.Memory;
using ScePSP.Threading.Synchronization;
using ScePSPUtils.Extensions;
using System;
using System.Collections.Generic;
using System.Threading;

namespace ScePSP.GE
{
    public unsafe class GEList : IContextInitialize
    {
        [Context]
        public GEBackEnd BackEnd;

        [Context]
        public GEConfig Config;

        [Context]
        public PspMemory Memory;

        [Context]
        public IGEConnector Connector;

        [Context]
        public DisplayConfig DisplayConfig;

        public enum WaitStatus
        {
            Pending = 0,
            AllSync = 1,
            Idle = 2
        }

        public GlobalGpuState GlobalGpuState = new GlobalGpuState();

        internal volatile LinkedList<GECore> Queue;

        public volatile AutoResetEvent QueueEvent = new AutoResetEvent(false);

        public WaitableStateMachine<WaitStatus> SyncStatus = new WaitableStateMachine<WaitStatus>();

        protected volatile Queue<GECore> FreeQueue;

        public const int GECoreCount = 64;

        public readonly GECore[] List = new GECore[GECoreCount];

        public volatile GECore First, Last, Current = null;

        public bool UsingGe { get; private set; }

        public bool Syncing = false;

        private GEList()
        {
        }

        void IContextInitialize.Initialize()
        {
            Queue = new LinkedList<GECore>();
            FreeQueue = new Queue<GECore>();
            for (int n = 0; n < GECoreCount; n++)
            {
                var GE = new GECore(Memory, this, n);
                List[n] = GE;
                EnqueueFree(List[n]);
            }
            SyncStatus.SetValue(WaitStatus.Idle);
        }

        public GECore Get(int Index)
        {
            lock (List) return List[Index];
        }

        public GECore DequeueFree()
        {
            GECore result;
            lock (FreeQueue)
            {
                result = FreeQueue.Dequeue();
                result.Available = false;
            }

            return result;
        }

        public void EnqueueFree(GECore GE)
        {
            lock (FreeQueue)
            {
                FreeQueue.Enqueue(GE);
                GE.SetFree();
            }
        }

        public void EnqueueFirst(GECore GE)
        {
            SyncStatus.SetValue(WaitStatus.Pending);
            lock (Queue)
            {
                Queue.AddFirst(GE);
                GE.SetQueued();
            }
            QueueEvent.Set();
        }

        public void Enqueue(GECore GE)
        {
            SyncStatus.SetValue(WaitStatus.Pending);
            lock (Queue)
            {
                Queue.AddLast(GE);
                GE.SetQueued();
            }
            QueueEvent.Set();
        }

        public void ProcessInit()
        {
        }

        public void ProcessStep()
        {
            Current = null;
            if (Queue.GetCountLock() > 0)
            {
                UsingGe = true;
                while (Queue.GetCountLock() > 0)
                {
                    Current = Queue.RemoveFirstAndGet();
                    if (First == null) First = Current;
                    Current.Process();
                    EnqueueFree(Current);
                    Last = Current;
                }
                Current = null;
                SyncStatus.SetValue(WaitStatus.AllSync);
            }
        }

        public void WaitSync(WaitStatus type, Action CallBack = null)
        {
            Syncing = true;
            SyncStatus.CallbackOnStateOnce(type, () =>
            {
                CallBack?.Invoke();
                Syncing = false;
            });
        }

        public void SetCurrent()
        {
            BackEnd.SetCurrent();
        }

        public void UnsetCurrent()
        {
            BackEnd.UnsetCurrent();
        }
    }
}
