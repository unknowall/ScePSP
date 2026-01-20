using ScePSP.Devices.Display;
using ScePSP.GE.State;
using ScePSP.Memory;
using ScePSPUtils.Extensions;
using System;
using System.Collections.Generic;
using System.Threading;

namespace ScePSP.GE
{
    public unsafe class GEList
    {
        public GEBackEnd BackEnd => PSPDrivers.GeBackEnd;

        public DisplayConfig Config => PSPDrivers.Config.DisplayConfig;

        public PspMemory Memory => PSPDrivers.PspMemory;

        public IGEConnector Connector => PSPDrivers.GEConnector;

        public GlobalGpuState GlobalGpuState = new GlobalGpuState();

        internal volatile LinkedList<GECore> Queue;

        public volatile AutoResetEvent QueueEvent = new AutoResetEvent(false);

        public enum WaitStatus
        {
            Pending = 0,
            AllSync = 1,
            Idle = 2
        }
        public WaitableStateMachine<WaitStatus> SyncStatus = new WaitableStateMachine<WaitStatus>();

        protected volatile Queue<GECore> FreeQueue;

        public const int GECoreCount = 64;

        public readonly GECore[] List = new GECore[GECoreCount];

        public volatile GECore First, Last, Current = null;

        public bool UsingGe { get; private set; }

        public bool Syncing = false;

        public GEList()
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

        public void WaitSync(Action CallBack = null)
        {
            Syncing = true;
            SyncStatus.CallbackOnStateOnce(WaitStatus.AllSync, () =>
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
