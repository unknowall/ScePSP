using ScePSP.GE.Run;
using ScePSP.GE.State;
using ScePSP.Memory;
using ScePSP.Threading.Synchronization;
using ScePSPUtils;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Threading;
using static ScePSP.Hle.Modules.ge.sceGe_user;

namespace ScePSP.GE
{
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 4)]
    public struct GpuInstruction
    {
        public uint Instruction;
        public OpCodes OpCode { get { return (OpCodes)((Instruction >> 24) & 0xFF); } }
        public uint Params { get { return ((Instruction) & 0xFFFFFF); } }
    }

    public sealed unsafe class GECore
    {
        private static readonly Logger Logger = Logger.GetLogger("GE");

        internal PspMemory Memory;

        public GpuStateStruct* GEStateStruct;

        private GERunner Runner;

        public delegate void GeCoreOpDelegate(GERunner Runner, OpCodes GpuOpCode, uint Params);

        private static readonly GeCoreOpDelegate InstructionSwitch = GECore.GenerateSwitch();

        public int Id;
        public GEList GeList;
        public volatile uint AddressStart;
        public volatile uint AddressCurrent;
        public volatile uint AddressStall;
        public volatile uint AddressEnd;

        public AutoResetEvent StatusSync = new AutoResetEvent(false);
        private GlobalGpuState GlobalGpuState;
        private readonly Stack<IntPtr> ExecutionStack = new Stack<IntPtr>();

        int MaxWaitCount = 0;
        public GEStatusEnum Status;
        public WaitableStateMachine<GEStatusEnum> WaitStatus = new WaitableStateMachine<GEStatusEnum>();

        public OptionalParams OptParam = new OptionalParams();
        public Stack<uint> CallStack = new Stack<uint>(0x100);
        public GeCallbackData Callbacks;
        public int CallbacksId = -1;

        public SignalBehavior Signal;

        int _primCount;

        public int[] CMDValues = new int[0xFFFF];

        public uint Pc;

        public bool StallReached
        {
            get { return AddressCurrent == AddressStall && AddressStall != 0; }
        }

        public bool Done, Available, Finish, Pause, OldSDK = false;

        internal GECore(PspMemory Memory, GEList GE, int Id)
        {
            this.Memory = Memory;
            this.GeList = GE;
            this.Id = Id;
            this.GlobalGpuState = GE.GlobalGpuState;
            this.Runner = new GERunner(this, GE.GlobalGpuState);
        }

        public void SetStartAddress(uint value, uint Stall)
        {
            uint Addr = value & PspMemory.MemoryMask;
            uint StallAddr = Stall & PspMemory.MemoryMask;
            AddressStart = Addr;
            AddressCurrent = Addr;
            AddressStall = StallAddr;
            Pc = AddressCurrent;
            Status = (Addr == StallAddr) ? GEStatusEnum.StallReached : GEStatusEnum.Queued;
            WaitStatus.SetValue(Status);

            GeList.BackEnd.Start(GEStateStruct);
        }

        public void SetStallAddress(uint value)
        {
            uint Addr = value & PspMemory.MemoryMask;
            if (Addr == AddressStall) return;
            //if (AddressStall != 0) SyncWaitStall();
            AddressStall = Addr;
            Sync();
        }

        public void SkipEnd()
        {
            var Current = AddressCurrent;

            var OP1 = *(GpuInstruction*)Memory.PspAddressToPointerUnsafe(Current);
            var OP2 = *(GpuInstruction*)Memory.PspAddressToPointerUnsafe(Current + 4);

            if (OP1.OpCode == OpCodes.FINISH && OP2.OpCode == OpCodes.END)
            {
                AddressCurrent += 8;
            }
        }

        public GEStatusEnum SyncStatus()
        {
            if (Status == GEStatusEnum.StallReached)
            {
                if (!StallReached)
                {
                    return GEStatusEnum.Drawing;
                }
            }
            return Status;
        }

        public bool WaitingOnStall()
        {
            return Status == GEStatusEnum.StallReached && MaxWaitCount > 0;
        }

        public void Sync()
        {
            StatusSync.Set();
        }

        public void WaitSyncStall()
        {
            //Console.Out.WriteLineColored(ConsoleColor.Red, "WaitSyncStall Start!");
            Status = GEStatusEnum.StallReached;
            WaitStatus.SetValue(Status);
            if (!StatusSync.WaitOne(2000))
            {
                MaxWaitCount++;
                if (MaxWaitCount > 60)
                {
                    Console.Out.WriteLineColored(ConsoleColor.Red, "WaitSyncStall too long, aborting the list {0}", this);
                }
            }
            else if (!StallReached)
            {
                Status = GEStatusEnum.Drawing;
                WaitStatus.SetValue(Status);
            }
        }

        public void WaitSyncPause()
        {
            Status = GEStatusEnum.EndReached;
            WaitStatus.SetValue(Status);
            if (!StatusSync.WaitOne(2000))
            {
                MaxWaitCount++;
                if (MaxWaitCount > 60)
                {
                    Console.Out.WriteLineColored(ConsoleColor.Red, "WaitSyncPause too long, aborting the list {0}", this);
                }
            }
            else
            {
                Status = GEStatusEnum.Drawing;
                WaitStatus.SetValue(Status);
            }
        }

        internal void Process()
        {
            //Console.WriteLine("Process() : {0} : 0x{1:X8} : 0x{2:X8} : 0x{3:X8}", Id, AddressCurrent, AddressStart, AddressStall);
            Finish = false;
            Done = false;
            Pause = false;
            MaxWaitCount = 0;
            Status = GEStatusEnum.Drawing;
            WaitStatus.SetValue(Status);
            while (!Done)
            {
                if (Pause) { WaitSyncPause(); }
                else
                if (StallReached) { WaitSyncStall(); }
                else
                {
                    MaxWaitCount = 0;
                    ProcessInstruction();
                }
            }
            Status = GEStatusEnum.Completed;
            WaitStatus.SetValue(Status);
        }

        private static GeCoreOpDelegate GenerateSwitch()
        {
            var DynamicMethod = new DynamicMethod("GECore.GenerateSwitch", typeof(void), new Type[] { typeof(GERunner), typeof(OpCodes), typeof(uint) });
            ILGenerator ILGenerator = DynamicMethod.GetILGenerator();
            var SwitchLabels = new Label[typeof(OpCodes).GetEnumValues().Length];
            var Names = typeof(OpCodes).GetEnumNames();
            for (int n = 0; n < SwitchLabels.Length; n++)
            {
                SwitchLabels[n] = ILGenerator.DefineLabel();
            }
            ILGenerator.Emit(System.Reflection.Emit.OpCodes.Ldarg_1);
            ILGenerator.Emit(System.Reflection.Emit.OpCodes.Switch, SwitchLabels);
            ILGenerator.Emit(System.Reflection.Emit.OpCodes.Ret);

            for (int n = 0; n < SwitchLabels.Length; n++)
            {
                ILGenerator.MarkLabel(SwitchLabels[n]);
                var MethodInfo_Operation = typeof(GERunner).GetMethod("OP_" + Names[n]);
                if (MethodInfo_Operation == null)
                {
                    Console.Error.WriteLine("Warning! Can't find GE.OpCode '" + Names[n] + "'");
                    MethodInfo_Operation = ((Action)GERunner.Methods.OP_UNKNOWN).Method;
                }
                if (MethodInfo_Operation.GetCustomAttributes(typeof(OPNotImplementedAttribute), true).Length > 0)
                {
                    var MethodInfo_Unimplemented = ((Action)GERunner.Methods.UNIMPLEMENTED_NOTICE).Method;
                    ILGenerator.Emit(System.Reflection.Emit.OpCodes.Ldarg_0);
                    //ILGenerator.Emit(OpCodes.Ldarg_1);
                    //ILGenerator.Emit(OpCodes.Ldarg_2);
                    ILGenerator.Emit(System.Reflection.Emit.OpCodes.Call, MethodInfo_Unimplemented);
                }
                {
                    ILGenerator.Emit(System.Reflection.Emit.OpCodes.Ldarg_0);
                    //ILGenerator.Emit(OpCodes.Ldarg_1);
                    //ILGenerator.Emit(OpCodes.Ldarg_2);
                    ILGenerator.Emit(System.Reflection.Emit.OpCodes.Call, MethodInfo_Operation);
                }
                ILGenerator.Emit(System.Reflection.Emit.OpCodes.Ret);
            }

            return (GeCoreOpDelegate)DynamicMethod.CreateDelegate(typeof(GeCoreOpDelegate));
        }

        internal GpuInstruction ReadInstructionAndMoveNext()
        {
            Pc = AddressCurrent;
            GpuInstruction Value = *(GpuInstruction*)Memory.PspAddressToPointerUnsafe(AddressCurrent);
            AddressCurrent += 4;

            return Value;
        }

        private void ProcessInstruction()
        {
            Runner.PC = AddressCurrent;
            var Instruction = ReadInstructionAndMoveNext();
            Runner.OpCode = Instruction.OpCode;
            Runner.Params24 = Instruction.Params;

            CMDValues[(int)Runner.OpCode] = (int)Instruction.Instruction & 0x00FFFFFF;

            InstructionSwitch(Runner, Instruction.OpCode, Instruction.Params);

            //if (Debug)
            //{
            //    var WritePC = Memory.GetPCWriteAddress(Runner.PC);
            //    Console.Error.WriteLine(
            //        "CODE(0x{0:X}-0x{1:X}) : PC(0x{2:X}) : {3} : 0x{4:X} : Done:{5}",
            //        AddressCurrent,
            //        AddressStall,
            //        WritePC,
            //        Instruction.OpCode,
            //        Instruction.Params,
            //        Done
            //    );
            //}
        }

        internal void JumpRelativeOffset(uint Address)
        {
            AddressCurrent = GEStateStruct->GetAddressRelativeToBaseOffset(Address);
        }

        internal void JumpAbsolute(uint Address)
        {
            AddressCurrent = Address;
        }

        public void CallAbsolute(uint Address)
        {
            CallStack.Push(AddressCurrent);
            CallStack.Push((uint)GEStateStruct->BaseOffset);
            JumpAbsolute(Address);
        }

        internal void CallRelativeOffset(uint Address)
        {
            CallStack.Push(AddressCurrent);
            CallStack.Push((uint)GEStateStruct->BaseOffset);
            JumpRelativeOffset(Address);
        }

        public uint GetAddressRel(uint Address)
        {
            return GEStateStruct->GetAddressRelativeToBase(Address);
        }

        public uint GetAddressRelOffset(uint Address)
        {
            return GEStateStruct->GetAddressRelativeToBaseOffset(Address);
        }

        internal void Ret()
        {
            if (CallStack.Count > 0)
            {
                GEStateStruct->BaseOffset = CallStack.Pop();
                JumpAbsolute(CallStack.Pop());
            }
            else
            {
                Console.Error.WriteLine("GE Stack is empty");
            }
        }

        public void SyncWait(GEStatusEnum Status, Action CallBack = null)
        {
            WaitStatus.CallbackOnStateOnce(Status, CallBack);
        }

        public Matrix4x4 GetMtx(PspGeMatrixTypes MatrixType)
        {
            Matrix4x4 Result;

            switch (MatrixType)
            {
                case PspGeMatrixTypes.Bone0:
                case PspGeMatrixTypes.Bone1:
                case PspGeMatrixTypes.Bone2:
                case PspGeMatrixTypes.Bone3:
                case PspGeMatrixTypes.Bone4:
                case PspGeMatrixTypes.Bone5:
                case PspGeMatrixTypes.Bone6:
                case PspGeMatrixTypes.Bone7:
                    Result = GEStateStruct->SkinningState.BoneMatrix(MatrixType - PspGeMatrixTypes.Bone0).Matrix4;
                    break;
                case PspGeMatrixTypes.World:
                    Result = GEStateStruct->VertexState.WorldMatrix.Matrix4;
                    break;
                case PspGeMatrixTypes.View:
                    Result = GEStateStruct->VertexState.ViewMatrix.Matrix4;
                    break;
                case PspGeMatrixTypes.Projection:
                    Result = GEStateStruct->VertexState.ProjectionMatrix.Matrix4;
                    break;
                case PspGeMatrixTypes.Texture:
                    Result = GEStateStruct->TextureMappingState.Matrix.Matrix4;
                    break;
                default:
                    Result = new Matrix4x4();
                    break;
            }

            return Result;
        }

        public void DoFinish(uint PC, uint Arg, bool ExecuteNow)
        {
            //Console.WriteLine("FINISH: Arg:{0}", Arg);
            if (Callbacks.FinishFunction != 0)
            {
                GeList.Connector.Finish(PC, Callbacks, Arg, ExecuteNow);
            }
        }

        public void DoSignal(uint PC, uint Signal, SignalBehavior Behavior, bool ExecuteNow)
        {
            Console.WriteLine("SIGNAL Callbacks {0} Signal {1} Behavior {2}", Callbacks.SignalFunction, Signal, Behavior);
            if (Callbacks.SignalFunction != 0)
            {
                GeList.Connector.Signal(PC, Callbacks, Signal, Behavior, ExecuteNow);
            }
        }

        public void SetQueued()
        {
            Status = GEStatusEnum.Queued;
            WaitStatus.SetValue(Status);
        }

        public void SetFree()
        {
            Available = true;
        }

        public void DeQueue()
        {
            Done = true;
            GeList.Queue.Remove(this);
            GeList.EnqueueFree(this);
        }
    }
}