using ScePSP.Core.GpuBackEnd.State;
using ScePSP.Core.Memory;
using ScePSPUtils.Extensions;
using System;
using System.Collections.Generic;
using System.Threading;

namespace ScePSP.Core.GpuBackEnd
{
    public unsafe class GpuProcessor
    {
        /*
         *   - GU_SYNC_FINISH - 0 - Wait until the last sceGuFinish command is reached
         *   - GU_SYNC_SIGNAL - 1 - Wait until the last (?) signal is executed
         *   - GU_SYNC_DONE   - 2 - Wait until all commands currently in list are executed
         *   - GU_SYNC_LIST   - 3 - Wait for the currently executed display list (GU_DIRECT)
         *   - GU_SYNC_SEND   - 4 - Wait for the last send list
         *   
         *   int sceGuSync(int mode, SyncTypeEnum what)
         *	 {
         *		 switch (mode)
         *		 {
         *			 case GU_SYNC_FINISH: return sceGeDrawSync(what);
         *			 case GU_SYNC_LIST  : return sceGeListSync(ge_list_executed[0], what);
         *			 case GU_SYNC_SEND  : return sceGeListSync(ge_list_executed[1], what);
         *		 	 default: case GU_SYNC_SIGNAL: case GU_SYNC_DONE: return 0;
         *	 	 }
         *	 }
         */
        /// <summary>
        /// Wait conditions for sceGeListSync() and sceGeDrawSync()
        /// </summary>
        public enum SyncTypeEnum : uint
        {
            ListDone = 0,

            ListQueued = 1,

            ListDrawingDone = 2,

            ListStallReached = 3,

            ListCancelDone = 4,
        }

        public GlobalGpuState GlobalGpuState = new GlobalGpuState();

        internal volatile LinkedList<GEProcess> GEProcessQueue;

        public volatile AutoResetEvent GEProcessQueueUpdated = new AutoResetEvent(false);

        protected volatile Queue<GEProcess> GEProcessFreeQueue;

        public const int GEProcessCount = 64;

        private readonly GEProcess[] GEProcessLists = new GEProcess[GEProcessCount];

        AutoResetEvent GEProcessreeEvent = new AutoResetEvent(false);

        public AutoResetEvent ListEnqueuedEvent = new AutoResetEvent(false);

        private volatile GEProcess CurrentGEProcess = null;

        private volatile GEProcess LastProcessedGEProcess = null;

        public bool UsingGe { get; private set; }

        public bool IsBreak = false;

        bool StartCapturingFrame = false;

        bool CapturingFrame = false;

        internal bool Syncing = false;

        public readonly WaitableStateMachine<Status2Enum> Status2 = new WaitableStateMachine<Status2Enum>(Status2Enum.Completed, Debug: false);

        public GpuBackEnd GpuBackEnd => PSPDrivers.GpuBackEnd;

        public GpuConfig GpuConfig => PSPDrivers.Config.GpuConfig;

        public PspMemory Memory => PSPDrivers.PspMemory;

        public IGpuConnector Connector => PSPDrivers.GpuConnector;

        public enum Status2Enum
        {
            Completed = 0,
            HavePendingLists = 1,
        }

        public GEProcess GetGEProcess(int Index)
        {
            lock (GEProcessLists) return GEProcessLists[Index];
        }

        public GpuProcessor()
        {
            GEProcessQueue = new LinkedList<GEProcess>();
            GEProcessFreeQueue = new Queue<GEProcess>();
            for (int n = 0; n < GEProcessCount; n++)
            {
                var GE = new GEProcess(Memory, this, n);
                GEProcessLists[n] = GE;
                //GEProcessFreeQueue.Enqueue(GEProcessLists[n]);
                EnqueueFreeGEProcess(GEProcessLists[n]);
            }
        }

        public GEProcess DequeueFreeGEProcess()
        {
            GEProcess result;
            //Console.WriteLine("DequeueFreeGEProcess: {0}", GEProcessFreeQueue.Count);
            lock (GEProcessFreeQueue)
            {
                result = GEProcessFreeQueue.Dequeue();
                result.Available = false;
            }

            return result;
        }

        public void EnqueueFreeGEProcess(GEProcess GE)
        {
            //Console.WriteLine("EnqueueFreeGEProcess: {0}", GEProcessFreeQueue.Count);
            lock (GEProcessFreeQueue)
            {
                GEProcessFreeQueue.Enqueue(GE);
                GE.SetFree();
            }
            GEProcessreeEvent.Set();
        }

        public void EnqueueGEProcessFirst(GEProcess GE)
        {
            //Console.WriteLine("EnqueueGEProcessFirst: {0}", GEProcessFreeQueue.Count);
            AddedGEProcess();
            lock (GEProcessQueue)
            {
                GEProcessQueue.AddFirst(GE);
                GE.SetQueued();
            }
            GEProcessQueueUpdated.Set();
            ListEnqueuedEvent.Set();
        }

        public void EnqueueGEProcessLast(GEProcess GE)
        {
            //Console.WriteLine("EnqueueGEProcessLast: {0}", GEProcessFreeQueue.Count);
            AddedGEProcess();
            lock (GEProcessQueue)
            {
                GEProcessQueue.AddLast(GE);
                GE.SetQueued();
            }
            GEProcessQueueUpdated.Set();
            ListEnqueuedEvent.Set();
        }

        public void ProcessInit()
        {
        }

        public void ProcessStep()
        {
            CurrentGEProcess = null;

            if (GEProcessQueue.GetCountLock() > 0)
            {
                UsingGe = true;
                while (GEProcessQueue.GetCountLock() > 0)
                {
                    CurrentGEProcess = GEProcessQueue.RemoveFirstAndGet();
                    CurrentGEProcess.SetDequeued();
                    CurrentGEProcess.Process();
                    EnqueueFreeGEProcess(CurrentGEProcess);
                    LastProcessedGEProcess = CurrentGEProcess;
                }
                CurrentGEProcess = null;

                Status2.SetValue(Status2Enum.Completed);
            }
        }

        protected void AddedGEProcess()
        {
            //Console.WriteLine("Running");
            Status2.SetValue(Status2Enum.HavePendingLists);
            GpuBackEnd.AddedGEProcess();
        }

        public void GeDrawSync(Action SyncCallback)
        {
            Syncing = true;
            Status2.CallbackOnStateOnce(Status2Enum.Completed, () =>
            {
                CapturingWaypoint();
                GpuBackEnd.Sync(LastProcessedGEProcess.GpuStateStructPointer);
                SyncCallback();
                Syncing = false;
            });
        }

        private void CapturingWaypoint()
        {
            if (CapturingFrame)
            {
                CapturingFrame = false;
                Console.WriteLine("EndCapturingFrame!");
                GpuBackEnd.EndCapture();
            }

            if (StartCapturingFrame)
            {
                StartCapturingFrame = false;
                CapturingFrame = true;
                GpuBackEnd.StartCapture();
                Console.WriteLine("StartCapturingFrame!");
            }
        }

        internal void MarkDepthBufferLoad()
        {
            //throw new NotImplementedException();
        }

        public void SetCurrent()
        {
            GpuBackEnd.SetCurrent();
        }

        public void UnsetCurrent()
        {
            GpuBackEnd.UnsetCurrent();
        }

        public void CaptureFrame()
        {
            StartCapturingFrame = true;
            Console.WriteLine("Waiting StartCapturingFrame!");
        }

        public int GeContinue()
        {
            throw new NotImplementedException();
        }

        public GEProcess GetCurrentGEProcess()
        {
            return CurrentGEProcess;
        }

        public GEProcesStatusEnum PeekStatus()
        {
            var GpuDisplayList = CurrentGEProcess;
            if (GpuDisplayList == null) return GEProcesStatusEnum.Completed;
            return GpuDisplayList.PeekStatus();
        }
    }

    public enum TextureLevelMode
    {
        Auto = 0,
        Const = 1,
        Slope = 2
    }

    public enum SyncTypeEnum : byte
    {
        WaitForCompletion = 0,
        Peek = 1,
    }

    public enum SignalBehavior : byte
    {
        PSP_GE_SIGNAL_NONE = 0x00,
        PSP_GE_SIGNAL_HANDLER_SUSPEND = 0x01,
        PSP_GE_SIGNAL_HANDLER_CONTINUE = 0x02,
        PSP_GE_SIGNAL_HANDLER_PAUSE = 0x03,
        PSP_GE_SIGNAL_SYNC = 0x08,
        PSP_GE_SIGNAL_JUMP = 0x10,
        PSP_GE_SIGNAL_CALL = 0x11,
        PSP_GE_SIGNAL_RET = 0x12,
        PSP_GE_SIGNAL_RJUMP = 0x13,
        PSP_GE_SIGNAL_RCALL = 0x14,
        PSP_GE_SIGNAL_OJUMP = 0x15,
        PSP_GE_SIGNAL_OCALL = 0x16,

        PSP_GE_SIGNAL_RTBP0 = 0x20,
        PSP_GE_SIGNAL_RTBP1 = 0x21,
        PSP_GE_SIGNAL_RTBP2 = 0x22,
        PSP_GE_SIGNAL_RTBP3 = 0x23,
        PSP_GE_SIGNAL_RTBP4 = 0x24,
        PSP_GE_SIGNAL_RTBP5 = 0x25,
        PSP_GE_SIGNAL_RTBP6 = 0x26,
        PSP_GE_SIGNAL_RTBP7 = 0x27,
        PSP_GE_SIGNAL_OTBP0 = 0x28,
        PSP_GE_SIGNAL_OTBP1 = 0x29,
        PSP_GE_SIGNAL_OTBP2 = 0x2A,
        PSP_GE_SIGNAL_OTBP3 = 0x2B,
        PSP_GE_SIGNAL_OTBP4 = 0x2C,
        PSP_GE_SIGNAL_OTBP5 = 0x2D,
        PSP_GE_SIGNAL_OTBP6 = 0x2E,
        PSP_GE_SIGNAL_OTBP7 = 0x2F,
        PSP_GE_SIGNAL_RCBP = 0x30,
        PSP_GE_SIGNAL_OCBP = 0x38,
        PSP_GE_SIGNAL_BREAK1 = 0xF0,
        PSP_GE_SIGNAL_BREAK2 = 0xFF,
    }

    public enum GEProcesStatusEnum
    {
        /// <summary>
        /// The list has been completed (PSP_GE_LIST_COMPLETED)
        /// </summary>
        Completed = 0,

        /// <summary>
        ///  list is queued but not executed yet (PSP_GE_LIST_QUEUED)
        /// </summary>
        Queued = 1,

        /// <summary>
        /// The list is currently being executed (PSP_GE_LIST_DRAWING)
        /// </summary>
        Drawing = 2,

        /// <summary>
        /// The list was stopped because it encountered stall address (PSP_GE_LIST_STALLING)
        /// </summary>
        Stalling = 3,

        /// <summary>
        /// The list is paused because of a signal or sceGeBreak (PSP_GE_LIST_PAUSED)
        /// </summary>
        Paused = 4,
    }

    public unsafe struct PspGeStack
    {
        public fixed uint Stack[8];
    }

    public unsafe struct PspGeListArgs
    {
        /// <summary>
        /// Size of the structure
        /// </summary>
        public uint Size;

        public uint GpuStateStructAddress;

        public uint NumberOfStacks;

        public uint StacksAddress;
    }
}