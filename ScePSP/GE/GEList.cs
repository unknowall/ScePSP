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

        protected volatile Queue<GECore> FreeQueue;

        public const int GECoreCount = 64;

        public readonly GECore[] List = new GECore[GECoreCount];

        public AutoResetEvent GEEvent = new AutoResetEvent(false);

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
            GEEvent.Set();
        }

        public void EnqueueFirst(GECore GE)
        {
            lock (Queue)
            {
                Queue.AddFirst(GE);
                GE.SetQueued();
            }
            QueueEvent.Set();
        }

        public void Enqueue(GECore GE)
        {
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
            }
        }

        public void WaitSync(GEStatusEnum type, Action CallBack = null)
        {
            Syncing = true;
            bool alldone = false;
            while (!alldone && PSPDrivers.Runing)
            {
                alldone = true;
                foreach (GECore GE in Queue)
                {
                    if (GE.Status != type)
                    {
                        alldone = false;
                        break;
                    }
                }
                Thread.Sleep(10);
            }
            if (BackEnd != null && Last != null) BackEnd.Sync(Last.GEStateStruct);
            CallBack?.Invoke();
            Syncing = false;
        }

        public void SetCurrent()
        {
            BackEnd.SetCurrent();
        }

        public void UnsetCurrent()
        {
            BackEnd.UnsetCurrent();
        }

        public int GeContinue()
        {
            return 0;
        }


    }
}
