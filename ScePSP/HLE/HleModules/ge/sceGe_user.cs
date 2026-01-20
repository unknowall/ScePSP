using ScePSP.Cpu;
using ScePSP.GE;
using ScePSP.GE.State;
using ScePSP.Hle.Attributes;
using ScePSP.Hle.Managers;
using ScePSP.Hle.Modules.sysmem;
using ScePSP.Memory;
using ScePSPUtils;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using static ScePSP.GE.GEList;

namespace ScePSP.Hle.Modules.ge
{
    [HlePspModule(ModuleFlags = ModuleFlags.UserMode | ModuleFlags.Flags0x00010011)]
    public unsafe partial class sceGe_user : HleModuleHost
    {
        static Logger Logger = Logger.GetLogger("sceGe");

        HleMemoryManager MemoryManager => PSPDrivers.HLE.MemoryManager;

        public GEList GeList => PSPDrivers.GEList;

        public SysMemUserForUser SysMemUserForUser => PSPDrivers.HleModules.SysMemUserForUser;

        private MemoryPartition GpuStateStructPartition = null;
        private GpuStateStruct* GpuStateStructPointer = null;
        private int eDRAMMemoryWidth;
        int CallbackLastId = 1;
        public Dictionary<int, GeCallbackData> Callbacks = new Dictionary<int, GeCallbackData>();

        protected override void ModuleInitialize()
        {
            GpuStateStructPartition = MemoryManager.GetPartition(MemoryPartitions.Kernel0).Allocate(
                sizeof(GpuStateStruct),
                Name: "GpuStateStruct"
            );
            GpuStateStructPointer = (GpuStateStruct*)GpuStateStructPartition.GetLowPointerSafe<GpuStateStruct>();
        }

        private int _sceGeListEnQueue(uint InstructionAddressStart, uint InstructionAddressStall, int CallbackId, PspGeListArgs* Args, bool Head)
        {
            var GE = GeList.DequeueFree();

            //Console.WriteLine($"_sceGeListEnQueue Start 0x{InstructionAddressStart & PspMemory.MemoryMask:X} Stall 0x{InstructionAddressStall & PspMemory.MemoryMask:X} ");

            if (SysMemUserForUser.sceKernelGetCompiledSdkVersion() < 0x02000000)
            {
                // Old games (i.e. having PSP SDK version < 2.00) are sometimes
                // reusing the same address for multiple lists, without waiting
                // TODO 得为老游戏做GE指令缓存

                //Console.WriteLine($"\nsceKernelGetCompiledSdkVersion {SysMemUserForUser.sceKernelGetCompiledSdkVersion()}\n");

                GE.OldSDK = true;

                //GE.SyncWaitDone(null);
            }

            GE.CallbacksId = -1;
            GE.Callbacks = default(GeCallbackData);

            if (CallbackId != -1)
            {
                GE.Callbacks = Callbacks[CallbackId];
                GE.CallbacksId = CallbackId;
            }

            if (Args != null && Args->GpuStateStructAddress != 0)
            {
                GE.OptParam.ContextAddress = (int)Args->GpuStateStructAddress;
                GE.OptParam.StackDepth = (int)Args->NumberOfStacks;
                GE.OptParam.StackAddress = (int)Args->StacksAddress;

                GE.GEStateStruct = (GpuStateStruct*)CpuProcessor.Memory.PspAddressToPointerSafe(Args->GpuStateStructAddress, Marshal.SizeOf(typeof(GpuStateStruct)));
            }

            if (GE.GEStateStruct == null)
            {
                GE.GEStateStruct = GpuStateStructPointer;
            }

            GE.SetStartAddress(InstructionAddressStart, InstructionAddressStall);

            if (Head)
            {
                GeList.EnqueueFirst(GE);
            }
            else GeList.Enqueue(GE);

            return GE.Id;
        }

        public enum PspGeMatrixTypes
        {
            Bone0 = 0,
            Bone1 = 1,
            Bone2 = 2,
            Bone3 = 3,
            Bone4 = 4,
            Bone5 = 5,
            Bone6 = 6,
            Bone7 = 7,
            World = 8,
            View = 9,
            Projection = 10,
            Texture = 11,
        }

        /// <summary>
        /// Get the address of VRAM.
        /// </summary>
        /// <returns>A pointer to the base of VRAM.</returns>
        [HlePspFunction(NID = 0xE47E40E4, FirmwareVersion = 150)]
        public uint sceGeEdramGetAddr()
        {
            return PspMemory.FrameBufferSegment.Low;
        }

        /// <summary>
        /// Get the size of VRAM.
        /// </summary>
        /// <returns>The size of VRAM (in bytes).</returns>
        [HlePspFunction(NID = 0x1F6752AD, FirmwareVersion = 150)]
        public int sceGeEdramGetSize()
        {
            return (int)PspMemory.FrameBufferSegment.Size;
        }

        /// <summary>
        /// Save the GE's current state. Save the GE's current state.
        /// </summary>
        /// <param name="ContextPtr">Pointer to a <see cref="PspGeContext"/>.</param>
        /// <returns>&lt; 0 on error.</returns>
        [HlePspFunction(NID = 0x438A385A, FirmwareVersion = 150)]
        public int sceGeSaveContext(GpuStateStruct* Context)
        {
            *Context = *this.GpuStateStructPointer;
            return 0;
        }

        /// <summary>
        /// Restore a previously saved GE context.
        /// </summary>
        /// <param name="contextAddr">Pointer to a <see cref="PspGeContext"/>.</param>
        /// <returns>&lt; 0 on error.</returns>
        [HlePspFunction(NID = 0x0BF608FB, FirmwareVersion = 150)]
        public int sceGeRestoreContext(GpuStateStruct* Context)
        {
            *GpuStateStructPointer = *Context;
            return 0;
        }

        /// <summary>
        /// Retrive the current value of a GE command.
        /// </summary>
        /// <param name="cmd">The GE command register to retrieve.</param>
        /// <returns>The value of the GE command.</returns>
        [HlePspFunction(NID = 0xDC93CFEF, FirmwareVersion = 150)]
        [HleTrackCall]
        public int sceGeGetCmd(int cmd)
        {
            var GE = GeList.Current;

            return GE.CMDValues[cmd];
        }

        /// <summary>
        /// Retrieve a matrix of the given type.
        /// </summary>
        /// <param name="MatrixType">One of <see cref="PspGeMatrixTypes"/>.</param>
        /// <param name="MatrixAddress">Pointer to a variable to store the matrix.</param>
        /// <returns>&lt; 0 on error.</returns>
        [HlePspFunction(NID = 0x57C8945B, FirmwareVersion = 150)]
        [HleTrackCall]
        public int sceGeGetMtx(PspGeMatrixTypes MatrixType, uint MatrixAddress)
        {
            if (MatrixType < 0 || MatrixType > PspGeMatrixTypes.Texture)
            {
                Console.WriteLine(string.Format("sceGeGetMtx invalid type mtxType={0:D}", MatrixType));
                return (int)SceKernelErrors.ERROR_INVALID_INDEX;
            }

            var GE = GeList.Current;

            Matrix4x4 val = GE.GetMtx(MatrixType);

            byte[] MatrixByte = new byte[64];

            fixed (byte* bytePtr = MatrixByte)
            {
                Marshal.StructureToPtr(val, (IntPtr)bytePtr, false);
            }

            Memory.WriteBytes(MatrixAddress, MatrixByte);

            return 0;
        }

        [HlePspFunction(NID = 0xB77905EA, FirmwareVersion = 150)]
        public int sceGeEdramSetAddrTranslation(int Size)
        {
            try
            {
                return eDRAMMemoryWidth;
            }
            finally
            {
                eDRAMMemoryWidth = Size;
            }
        }

        /// <summary>
        /// Interrupt drawing queue.
        /// </summary>
        /// <param name="Mode">If set to 1, reset all the queues.</param>
        /// <param name="BreakAddress">Unused (just K1-checked).</param>
        /// <returns>The stopped queue ID if mode isn't set to 0, otherwise 0, and &lt; 0 on error.</returns>
        [HlePspFunction(NID = 0xB448EC0D, FirmwareVersion = 150)]
        [HleTrackCall]
        public int sceGeBreak(int Mode, void* BreakAddress)
        {
            var GE = GeList.Current;

            if (GE == null)
            {
                return 0;
            }

            int result = 0;

            if (Mode == 0)
            { // Pause the current list only.
                if (GE != null)
                {
                    GE.Pause = true;
                    result = GE.Id;
                }
            }
            else if (Mode == 1)
            { // Pause the current list and cancel the rest of the queue.
                if (GE != null)
                {
                    GE.Pause = true;
                    for (int i = 0; i < GEList.GECoreCount; i++)
                    {
                        GeList.List[i].Status = GEStatusEnum.Cancel;
                    }
                    return GE.Id;
                }
            }

            return result;
        }

        /// <summary>
        /// Restart drawing queue.
        /// </summary>
        /// <returns>&lt; 0 on error.</returns>
        [HlePspFunction(NID = 0x4C06E472, FirmwareVersion = 150)]
        [HleTrackCall]
        public int sceGeContinue()
        {
            var GE = GeList.Current;

            if (GE == null)
            {
                return 0;
            }

            lock (this)
            {
                if (GE.Status == GEStatusEnum.EndReached)
                {
                    GE.SkipEnd();
                }
                GE.Sync();
            }

            return 0;
        }

        /// <summary>
        /// Register callback handlers for the the Ge
        /// </summary>
        /// <param name="PspGeCallbackData">Configured callback data structure</param>
        /// <returns>The callback ID, less than 0 on error</returns>
        [HlePspFunction(NID = 0xA4FC06A4, FirmwareVersion = 150)]
        [HleTrackCall]
        public int sceGeSetCallback(ref GeCallbackData PspGeCallbackData)
        {
            int CallbackId = CallbackLastId++;

            Callbacks[CallbackId] = PspGeCallbackData;

            var CallbackData = PspGeCallbackData;

            /*
            ConsoleUtils.SaveRestoreConsoleColor(ConsoleColor.Cyan, () =>
            {
                Console.WriteLine("PspGeCallbackData.Finish(0x{0:X}) : (0x{1:X})", CallbackData.FinishFunction, CallbackData.FinishArgument);
                Console.WriteLine("PspGeCallbackData.Signal(0x{0:X}) : (0x{1:X})", CallbackData.SignalFunction, CallbackData.SignalArgument);
            });
            */

            Logger.Info("PspGeCallbackData.Finish(0x{0:X}) : (0x{1:X})", PspGeCallbackData.FinishFunction, PspGeCallbackData.FinishArgument);
            Logger.Info("PspGeCallbackData.Signal(0x{0:X}) : (0x{1:X})", PspGeCallbackData.SignalFunction, PspGeCallbackData.SignalArgument);

            return CallbackId;
        }

        /// <summary>
        /// Unregister the callback handlers
        /// </summary>
        /// <param name="cbid">The ID of the callbacks from sceGeSetCallback</param>
        /// <returns>Less than 0 on error</returns>
        [HlePspFunction(NID = 0x05DB22CE, FirmwareVersion = 150)]
        [HleTrackCall]
        public int sceGeUnsetCallback(int cbid)
        {
            Callbacks.Remove(cbid);
            return 0;
        }

        /// <summary>
        /// Enqueue a display list at the tail of the GE display list queue.
        /// </summary>
        /// <param name="InstructionAddressStart">The head of the list to queue.</param>
        /// <param name="InstructionAddressStall">The stall address. If NULL then no stall address set and the list is transferred immediately.</param>
        /// <param name="CallbackId">ID of the callback set by calling <see cref="sceGeSetCallback"/></param>
        /// <param name="Args">Structure containing GE context buffer address</param>
        /// <returns>The DisplayList ID</returns>
        [HlePspFunction(NID = 0xAB49E76A, FirmwareVersion = 150)]
        [HleTrackCall]
        public int sceGeListEnQueue(uint InstructionAddressStart, uint InstructionAddressStall, int CallbackId, PspGeListArgs* Args)
        {
            return _sceGeListEnQueue(InstructionAddressStart, InstructionAddressStall, CallbackId, Args, false);
        }

        /// <summary>
        /// Enqueue a display list at the head of the GE display list queue.
        /// </summary>
        /// <param name="InstructionAddressStart">The head of the list to queue.</param>
        /// <param name="InstructionAddressStall">The stall address. If NULL then no stall address set and the list is transferred immediately.</param>
        /// <param name="CallbackId">ID of the callback set by calling <see cref="sceGeSetCallback"/></param>
        /// <param name="Args">Structure containing GE context buffer address</param>
        /// <returns>The DisplayList ID</returns>
        [HlePspFunction(NID = 0x1C0D95A6, FirmwareVersion = 150)]
        [HleTrackCall]
        public int sceGeListEnQueueHead(uint InstructionAddressStart, uint InstructionAddressStall, int CallbackId, PspGeListArgs* Args)
        {
            return _sceGeListEnQueue(InstructionAddressStart, InstructionAddressStall, CallbackId, Args, true);
        }

        /// <summary>
        /// Cancel a queued or running list.
        /// </summary>
        /// <param name="DisplayListId">A DisplayList ID</param>
        /// <returns>&lt; 0 on error.</returns>
        [HlePspFunction(NID = 0x5FB86AB0, FirmwareVersion = 150)]
        [HleTrackCall]
        public int sceGeListDeQueue(int GEProcessID)
        {
            var GE = GeList.Get(GEProcessID);
            GE.DeQueue();
            return 0;
        }

        /// <summary>
        /// Update the stall address for the specified queue.
        /// </summary>
        /// <param name="DisplayListId">The ID of the queue.</param>
        /// <param name="InstructionAddressStall">The stall address to update</param>
        /// <returns>Unknown. Probably 0 if successful. &lt; 0 on error</returns>
        [HlePspFunction(NID = 0xE0D68148, FirmwareVersion = 150)]
        //[HleTrackCall]
        public int sceGeListUpdateStallAddr(int GEProcessID, uint InstructionAddressStall)
        {
            lock (this)
            {
                var GE = GeList.Get(GEProcessID);
                GE.SetStallAddress(InstructionAddressStall);
            }
            return 0;
        }

        [HlePspFunction(NID = 0x03444EB4, FirmwareVersion = 150)]
        [HleTrackCall]
        public int sceGeListSync(int GEProcessID, int Mode)
        {
            var GE = GeList.Get(GEProcessID);

            int result = 0;
            GEStatusEnum WaitAt = GEStatusEnum.Completed;
            if (Mode == 0 && GE.Done)
            {
                //GE.SyncWait(WaitAt);
                return 0;
            }
            else if (Mode == 1)
            {
                WaitAt = GEStatusEnum.StallReached;
                //GE.SyncWaitStall(WaitAt);
                result = (int)GE.SyncStatus();
            }

            if (ThreadManager.Current != null)
                ThreadManager.Current.SetWaitAndPrepareWakeUp(
                        HleThread.WaitType.GraphicEngine, "sceGeListSync",
                        GE,
                        (WakeUp) => { GE.SyncWait(WaitAt, WakeUp); }
                );

            return result;
        }

        [HlePspFunction(NID = 0xB287BD61, FirmwareVersion = 150, CheckInsideInterrupt = false)]
        [HleTrackCall]
        public int sceGeDrawSync(int Mode)
        {
            int result = 0;
            if (Mode == 0)
            {
                if (ThreadManager.Current != null)
                    ThreadManager.Current.SetWaitAndPrepareWakeUp(
                            HleThread.WaitType.GraphicEngine, "sceGeDrawSync",
                            GeList,
                            (WakeUp) => { GeList.WaitSync(WakeUp); }
                    );
                //GeList.WaitSync(null);
            }
            else if (Mode == 1)
            {
                //GeList.WaitSync( null);
                result = (int)GeList.Last.SyncStatus();
            }

            return result;
        }
    }
}
