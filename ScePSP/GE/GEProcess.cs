#define PRIM_BATCH

using LightGL;
using ScePSP.Core.Cpu;
using ScePSP.Core.GpuBackEnd.State;
using ScePSP.Core.GpuBackEnd.VertexReading;
using ScePSP.Core.Memory;
using ScePSPUtils;
using ScePSPUtils.Extensions;
using System;
using System.Collections.Generic;
using System.Drawing.Design;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security.Cryptography.Xml;
using System.Threading;
using static ScePSP.Core.GpuBackEnd.GpuBackEnd;

namespace ScePSP.Core.GpuBackEnd
{
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 4)]
    public struct GpuInstruction
    {
        public uint Instruction;

        public GpuOpCodes OpCode => (GpuOpCodes)((Instruction >> 24) & 0xFF);

        public uint Params => Instruction & 0xFFFFFF;

        public override string ToString() => $"GpuInstruction({OpCode}, {Params})";
    }

    public sealed unsafe class GEProcess
    {
        public static uint[] DummyData = new uint[GpuStateStruct.StructSizeInBytes];

        private static readonly Logger Logger = Logger.GetLogger("GE");

        public struct OptionalParams
        {
            public int ContextAddress;
            public int StackDepth;
            public int StackAddress;
        }

        public int Id;
        public GpuProcessor GpuProcessor;
        private volatile uint InstructionAddressStart;
        private volatile uint InstructionAddressCurrent;
        private volatile uint InstructionAddressStall;
        AutoResetEvent StallAddressUpdated = new AutoResetEvent(false);
        public GpuStateStruct GpuStateStructPointer = new GpuStateStruct(new GpuStateData(DummyData));
        public GpuStateData GpuStateData => GpuStateStructPointer.data;
        private GlobalGpuState GlobalGpuState;
        private readonly Stack<IntPtr> ExecutionStack = new Stack<IntPtr>();
        public readonly WaitableStateMachine<GEProcesStatusEnum> Status = new WaitableStateMachine<GEProcesStatusEnum>();
        public bool Available { set; get; }
        public OptionalParams pspGeListOptParam;
        internal bool Done;
        public Stack<uint> CallStack = new Stack<uint>(0x10);

        internal PspMemory Memory;

        //PspWaitEvent OnFreed = new PspWaitEvent();
        //public enum Status2Enum
        //{
        //	Drawing,
        //	Free,
        //}
        //public readonly WaitableStateMachine<Status2Enum> Status2 = new WaitableStateMachine<Status2Enum>(Debug: false);

        public PspGeCallbackData Callbacks;
        public int CallbacksId;

        public SignalBehavior Signal;

        int _primCount;

        //Action[] InstructionSwitch = new Action[256];

        internal GEProcess(PspMemory Memory, GpuProcessor GpuProcessor, int Id)
        {
            this.Memory = Memory;
            this.GpuProcessor = GpuProcessor;
            this.Id = Id;
            GlobalGpuState = GpuProcessor.GlobalGpuState;
        }

        public void SetInstructionAddressStartAndCurrent(uint value)
        {
            InstructionAddressCurrent = value & PspMemory.MemoryMask;
            InstructionAddressStart = value & PspMemory.MemoryMask;
        }

        public void SetInstructionAddressStall(uint value)
        {
            uint addr = value & PspMemory.MemoryMask;
            if (addr != 0 && !PspMemory.IsAddressValid(addr))
            {
                throw new InvalidOperationException($"Invalid StallAddress! 0x{addr}");
            }
            if (addr != InstructionAddressStall)
            {
                InstructionAddressStall = addr;
                StallAddressUpdated.Set();
            }
        }

        internal void Process()
        {
            Status.SetValue(GEProcesStatusEnum.Drawing);

            //Console.WriteLine("Process() : {0} : 0x{1:X8} : 0x{2:X8} : 0x{3:X8}", Id,
            //    InstructionAddressCurrent, InstructionAddressStart, InstructionAddressStall);

            Done = false;
            while (!Done)
            {
                //Console.WriteLine($"Process({Id}) Current 0x{InstructionAddressCurrent:X} Start 0x{InstructionAddressStart:X} Stall 0x{InstructionAddressStall:X} ");
                if (InstructionAddressStall != 0 && InstructionAddressCurrent >= InstructionAddressStall)
                {
                    Status.SetValue(GEProcesStatusEnum.Stalling);
                    do
                    {
                        if (!StallAddressUpdated.WaitOne(1000))
                        {
                            ConsoleUtils.SaveRestoreConsoleColor(ConsoleColor.Magenta, () =>
                            {
                                Console.WriteLine($"GEProcessQueue.GetCountLock: {GpuProcessor.GEProcessQueue.GetCountLock()}");
                                Console.WriteLine($"CurrentGEProcess.Status: {Status.ToStringDefault()}");
                            });
                            if (GpuProcessor.Syncing)
                            {
                                Done = true;
                                Status.SetValue(GEProcesStatusEnum.Completed);
                                return;
                            }
                        }
                    } while (InstructionAddressStall != 0 && InstructionAddressCurrent >= InstructionAddressStall);

                }

                ProcessInstruction();
            }

            Status.SetValue(GEProcesStatusEnum.Completed);
        }

        internal GpuInstruction ReadInstructionAndMoveNext()
        {
            var Value = *(GpuInstruction*)Memory.PspAddressToPointerUnsafe(InstructionAddressCurrent);

            InstructionAddressCurrent += 4;

            return Value;
        }

        uint Pc => InstructionAddressCurrent;

        //uint _bjumpCount = 0;

        private GuPrimitiveType _lastPrimType, _lastPPrimType;

        private void ProcessInstruction()
        {
            var Instruction = ReadInstructionAndMoveNext();

            var Params24 = Instruction.Params;

            //Console.WriteLine($"ProcessInstruction: Pc={Pc}, Instruction={Instruction}");

            //InstructionSwitch(GpuDisplayListRunner, Instruction.OpCode, Instruction.Params);

            GpuStateStructPointer.data[Instruction.OpCode] = Instruction.Params;

            switch (Instruction.OpCode)
            {
                // Address
                case GpuOpCodes.ORIGIN_ADDR:
                    {
                        break;
                    }

                case GpuOpCodes.OFFSET_ADDR:
                    {
                        GpuStateData[GpuOpCodes.OFFSET_ADDR] = Params24 << 8;
                        break;
                    }

                case GpuOpCodes.BBOX:
                    {
                        Console.Out.WriteLineColored(ConsoleColor.Red, $" GpuOpCodes BBOX ");
                        break;
                    }

                // Flow
                case GpuOpCodes.JUMP:
                    {
                        JumpRelativeOffset((uint)(Params24 & ~3));
                        break;
                    }

                case GpuOpCodes.BJUMP:
                    {
                        //Console.Out.WriteLineColored(ConsoleColor.Red, $"BJUMP to {(Params24 & ~3)}");
                        //if (_bjumpCount++ < 5)
                        //{
                            //JumpRelativeOffset((uint)(Params24 & ~3));
                        //}
                        //else
                        //{
                        //    Console.Out.WriteLineColored(ConsoleColor.Red, "GE - BJUMP Loop > 5");
                        //}
                        break;
                    }

                case GpuOpCodes.CALL:
                    {
                        CallRelativeOffset((uint)(Params24 & ~3));
                        break;
                    }

                case GpuOpCodes.RET:
                    {
                        Ret();
                        break;
                    }

                // Finishing
                case GpuOpCodes.END:
                    {
                        Done = true;
                        GpuProcessor.GpuBackEnd.End(GpuStateStructPointer);
                        break;
                    }

                case GpuOpCodes.FINISH:
                    {
                        GpuProcessor.GpuBackEnd.Finish(GpuStateStructPointer);
                        DoFinish(InstructionAddressCurrent, Params24, ExecuteNow: true);
                        break;
                    }

                // Texture
                case GpuOpCodes.TFLUSH:
                    {
                        //Console.Out.WriteLineColored(ConsoleColor.Green, $"GpuOpCodes.TFLUSH");
                        GpuProcessor.GpuBackEnd.TextureFlush(GpuStateStructPointer);
                        break;
                    }

                case GpuOpCodes.TSYNC:
                    {
                        //Console.Out.WriteLineColored(ConsoleColor.Green, $"GpuOpCodes.TSYNC");
                        GpuProcessor.GpuBackEnd.TextureSync(GpuStateStructPointer);
                        break;
                    }

                case GpuOpCodes.TRXKICK:
                    {
                        var transfer = GpuStateStructPointer.TextureTransferState;
                        transfer.TexelSize = (TextureTransferStateStruct.TexelSizeEnum)Params24.Extract(0, 1);

                        Console.Out.WriteLineColored(ConsoleColor.Green, $"TRXKICK: TexelSize {transfer.TexelSize}");

                        GpuProcessor.GpuBackEnd.Transfer(GpuStateStructPointer);
                        break;
                    }

                case GpuOpCodes.BEZIER:
                    {
                        var uCount = (byte)Params24.Extract(0, 8);
                        var vCount = (byte)Params24.Extract(8, 8);
                        DrawBezier(uCount, vCount);
                        break;
                    }

                case GpuOpCodes.SPLINE:
                    {
                        var sp_ucount = (int)Params24.Extract(0, 8);
                        var sp_vcount = (int)Params24.Extract(8, 8);
                        var sp_utype = (int)Params24.Extract(16, 2);
                        var sp_vtype = (int)Params24.Extract(18, 2);
                        //Console.WriteLine("OP_SPLINE(%d, %d, %d, %d)", sp_ucount, sp_vcount, sp_utype, sp_vtype);
                        DrawSpline(sp_ucount, sp_vcount, sp_utype, sp_vtype);
                        break;
                    }

                case GpuOpCodes.PPRIM:
                    {
                        var PrimitiveType = (GuPrimitiveType)Params24.Extract(16, 3);
                        var vertexCount = (ushort)Params24.Extract(0, 16);

                        if (vertexCount == 0) return;

                        if (PrimitiveType != GuPrimitiveType.ContinuePreviousPrim)
                            _lastPPrimType = PrimitiveType;

                        //Console.Out.WriteLineColored(ConsoleColor.Cyan, $"PPRIM: Type {PrimitiveType} VertexCount {vertexCount}");

                        //if (_primCount == 0)
                        //{
                        //    GpuProcessor.GpuBackEnd.BeforeDraw(GpuStateStructPointer);
                        //    GpuProcessor.GpuBackEnd.PrimStart(GlobalGpuState, GpuStateStructPointer, _lastPPrimType);
                        //}

                        //if (vertexCount > 0)
                        //{
                        //    GpuProcessor.GpuBackEnd.Prim(vertexCount, true);
                        //}

                        //var nextInstruction = *(GpuInstruction*)Memory.PspAddressToPointerUnsafe(Pc + 4);
                        //if (nextInstruction.OpCode == GpuOpCodes.PPRIM &&
                        //    (GuPrimitiveType)nextInstruction.Params.Extract(16, 3) == PrimitiveType)
                        //{
                        //    _primCount++;
                        //}
                        //else
                        //{
                        //    _primCount = 0;
                        //    GpuProcessor.GpuBackEnd.PrimEnd();
                        //}
                        break;
                    }

                case GpuOpCodes.PRIM:
                    {
                        var PrimitiveType = (GuPrimitiveType)Params24.Extract(16, 3);
                        var vertexCount = (ushort)Params24.Extract(0, 16);

                        if (vertexCount == 0) return;

                        if (PrimitiveType != GuPrimitiveType.ContinuePreviousPrim)
                            _lastPPrimType = PrimitiveType;

                        //Console.Out.WriteLineColored(ConsoleColor.Cyan, $"PRIM: Type {PrimitiveType} VertexCount {vertexCount}");
#if PRIM_BATCH
                        var nextInstruction = *(GpuInstruction*)Memory.PspAddressToPointerUnsafe(Pc + 4);

                        if (_primCount == 0)
                        {
                            GpuProcessor.GpuBackEnd.BeforeDraw(GpuStateStructPointer);
                            GpuProcessor.GpuBackEnd.PrimStart(GlobalGpuState, GpuStateStructPointer, _lastPPrimType);
                        }

                        if (vertexCount > 0)
                        {
                            GpuProcessor.GpuBackEnd.Prim(vertexCount);
                        }

                        if (nextInstruction.OpCode == GpuOpCodes.PRIM &&
                            (GuPrimitiveType)nextInstruction.Params.Extract(16, 3) == PrimitiveType)
                        {
                            _primCount++;
                        }
                        else
                        {
                            _primCount = 0;
                            GpuProcessor.GpuBackEnd.PrimEnd();
                        }
#else
                    GpuDisplayList.GpuProcessor.GpuImpl.BeforeDraw(GpuDisplayList.GpuStateStructPointer);
                    GpuDisplayList.GpuProcessor.GpuImpl.PrimStart(GlobalGpuState, GpuDisplayList.GpuStateStructPointer);
                    GpuDisplayList.GpuProcessor.GpuImpl.Prim(GlobalGpuState, GpuDisplayList.GpuStateStructPointer, primitiveType, vertexCount);
                    GpuDisplayList.GpuProcessor.GpuImpl.PrimEnd(GlobalGpuState, GpuDisplayList.GpuStateStructPointer);
#endif
                        break;
                    }

                case GpuOpCodes.ZBW:
                    {
                        GpuProcessor.MarkDepthBufferLoad(); // @TODO: Is this required?
                        break;
                    }

                case GpuOpCodes.SIGNAL:
                    {
                        var signal = Params24.Extract(0, 16);
                        var behaviour = (SignalBehavior)Params24.Extract(16, 8);

                        //Console.Out.WriteLineColored(ConsoleColor.Green, "OP_SIGNAL: {0}, {1}", signal, behaviour);

                        switch (behaviour)
                        {
                            case SignalBehavior.PSP_GE_SIGNAL_NONE:
                                break;
                            case SignalBehavior.PSP_GE_SIGNAL_HANDLER_CONTINUE:
                            case SignalBehavior.PSP_GE_SIGNAL_HANDLER_PAUSE:
                            case SignalBehavior.PSP_GE_SIGNAL_HANDLER_SUSPEND:
                                var next = ReadInstructionAndMoveNext();
                                if (next.OpCode != GpuOpCodes.END)
                                {
                                    throw new NotImplementedException("Error! Next Signal not an END! : " + next.OpCode);
                                }
                                break;
                            default:
                                throw new NotImplementedException($"Not implemented {behaviour}");
                        }

                        DoSignal(Pc, signal, behaviour, ExecuteNow: true);

                        break;
                    }

                // Matrices
                case GpuOpCodes.TMS:
                    {
                        GpuStateData[GpuOpCodes.TMS] = 0;
                        break;
                    }
                case GpuOpCodes.TMATRIX:
                    {
                        var pos = GpuStateData[GpuOpCodes.TMS]++;
                        GpuStateData[GpuOpCodes.TMATRIX_BASE + (ushort)pos] = Params24 << 8;
                        break;
                    }
                case GpuOpCodes.VMS:
                    {
                        GpuStateData[GpuOpCodes.VMS] = 0;
                        break;
                    }
                case GpuOpCodes.VIEW:
                    {
                        var pos = GpuStateData[GpuOpCodes.VMS]++;
                        GpuStateData[GpuOpCodes.VIEW_MATRIX_BASE + (ushort)pos] = Params24 << 8;
                        break;
                    }
                case GpuOpCodes.WMS:
                    {
                        GpuStateData[GpuOpCodes.WMS] = 0;
                        break;
                    }
                case GpuOpCodes.WORLD:
                    {
                        var pos = GpuStateData[GpuOpCodes.WMS]++;
                        GpuStateData[GpuOpCodes.WORLD_MATRIX_BASE + (ushort)pos] = Params24 << 8;
                        break;
                    }
                case GpuOpCodes.PMS:
                    {
                        GpuStateData[GpuOpCodes.PMS] = 0;
                        break;
                    }
                case GpuOpCodes.PROJ:
                    {
                        var pos = GpuStateData[GpuOpCodes.PMS]++;
                        GpuStateData[GpuOpCodes.PROJ_MATRIX_BASE + (ushort)pos] = Params24 << 8;
                        break;
                    }
                case GpuOpCodes.BOFS:
                    {
                        GpuStateData[GpuOpCodes.BOFS] = Params24;
                        break;
                    }
                case GpuOpCodes.BONE:
                    {
                        var pos = GpuStateData[GpuOpCodes.BOFS]++;
                        GpuStateData[GpuOpCodes.BONE_MATRIX_BASE + (ushort)pos] = Params24 << 8;
                        break;
                    }

                    //default:
                    //    Console.Error.WriteLine("Unknown GE OpCode Instruction: PC=0x{0:X8} OpCode={1} Params=0x{2:X6}",
                    //        Pc - 4, Instruction.OpCode, Instruction.Params);
                    //    break;
            }

            //var WritePC = Memory.GetPCWriteAddress(Pc);
            //Console.Error.WriteLine(
            //    "CODE(0x{0:X}-0x{1:X}) : PC(0x{2:X}) : {3} : 0x{4:X} : Done:{5}",
            //    InstructionAddressCurrent,
            //    InstructionAddressStall,
            //    WritePC,
            //    Instruction.OpCode,
            //    Instruction.Params,
            //    Done
            //);
        }

        private static float[] BernsteinCoeff(float u)
        {
            var uPow2 = u * u;
            var uPow3 = uPow2 * u;
            var u1 = 1 - u;
            var u1Pow2 = u1 * u1;
            var u1Pow3 = u1Pow2 * u1;

            return new[]
            {
                u1Pow3,
                3 * u * u1Pow2,
                3 * uPow2 * u1,
                uPow3,
            };
        }

        private static void PointMultAdd(ref VertexInfo dest, ref VertexInfo src, float f)
        {
            dest.Position += src.Position * f;
            dest.Texture += src.Texture * f;
            dest.Color += src.Color * f;
            dest.Normal += src.Normal * f;
        }

        private VertexInfo[,] GetControlPoints(int uCount, int vCount)
        {
            var controlPoints = new VertexInfo[uCount, vCount];

            var vertexPtr =
                (byte*)GpuProcessor.Memory.PspAddressToPointerSafe(
                    GpuStateStructPointer.GetAddressRelativeToBaseOffset(GpuStateStructPointer.VertexAddress));
            var vertexReader = new VertexReader();
            vertexReader.SetVertexTypeStruct(GpuStateStructPointer.VertexState.Type, vertexPtr);

            for (var u = 0; u < uCount; u++)
            {
                for (var v = 0; v < vCount; v++)
                {
                    controlPoints[u, v] = vertexReader.ReadVertex(v * uCount + u);
                    //Console.WriteLine("getControlPoints({0}, {1}) : {2}", u, v, controlPoints[u, v]);
                }
            }
            return controlPoints;
        }

        private void DrawBezier(int uCount, int vCount)
        {
            var divS = GpuStateStructPointer.PatchState.DivS;
            var divT = GpuStateStructPointer.PatchState.DivT;

            if ((uCount - 1) % 3 != 0 || (vCount - 1) % 3 != 0)
            {
                Logger.Warning("Unsupported bezier parameters ucount=" + uCount + " vcount=" + vCount);
                return;
            }
            if (divS <= 0 || divT <= 0)
            {
                Logger.Warning("Unsupported bezier patches patch_div_s=" + divS + " patch_div_t=" + divT);
                return;
            }

            var anchors = GetControlPoints(uCount, vCount);

            var patch = new VertexInfo[divS + 1, divT + 1];

            var upcount = uCount / 3;
            var vpcount = vCount / 3;

            var ucoeff = new float[divS + 1][];

            for (var j = 0; j <= divT; j++)
            {
                var vglobal = (float)j * vpcount / divT;

                var vpatch = (int)vglobal;
                var v = vglobal - vpatch;
                if (j == divT)
                {
                    vpatch--;
                    v = 1.0f;
                }
                var vcoeff = BernsteinCoeff(v);

                for (var i = 0; i <= divS; i++)
                {
                    var uglobal = (float)i * upcount / divS;
                    var upatch = (int)uglobal;
                    var u = uglobal - upatch;
                    if (i == divS)
                    {
                        upatch--;
                        u = 1.0f;
                    }
                    ucoeff[i] = BernsteinCoeff(u);

                    var p = default(VertexInfo);
                    p.Position = Vector4.Zero;
                    p.Normal = Vector4.Zero;

                    for (var ii = 0; ii < 4; ++ii)
                    {
                        for (var jj = 0; jj < 4; ++jj)
                        {
                            PointMultAdd(
                                ref p,
                                ref anchors[3 * upatch + ii, 3 * vpatch + jj],
                                ucoeff[i][ii] * vcoeff[jj]
                            );
                        }
                    }

                    p.Texture.X = uglobal;
                    p.Texture.Y = vglobal;

                    patch[i, j] = p;
                }
            }

            GpuProcessor.GpuBackEnd.BeforeDraw(GpuStateStructPointer);
            GpuProcessor.GpuBackEnd.DrawCurvedSurface(GlobalGpuState, GpuStateStructPointer, patch, uCount, vCount);
        }

        private static float[] LinearCoeff(float t)
        {
            return new[]
            {
                1 - t, // 第一个控制点权重
                t      // 第二个控制点权重
            };
        }

        internal void DrawSpline(int sp_ucount, int sp_vcount, int sp_utype, int sp_vtype)
        {
            var divS = GpuStateStructPointer.PatchState.DivS;
            var divT = GpuStateStructPointer.PatchState.DivT;

            if (sp_ucount <= 0 || sp_vcount <= 0 || divS <= 0 || divT <= 0)
                return;

            int normalizedUType = sp_utype == 0 ? 0 : 1;
            int normalizedVType = sp_vtype == 0 ? 0 : 1;
            bool isUCubic = normalizedUType == 1;
            bool isVCubic = normalizedVType == 1;

            var controlPoints = GetControlPoints(sp_ucount, sp_vcount);
            var splinePatch = new VertexInfo[divS + 2, divT + 2];

            int uControlStep = isUCubic ? 3 : 1;
            int vControlStep = isVCubic ? 3 : 1;

            for (int j = 0; j <= divT + 1; j++)
            {
                float vGlobal = (float)j * (divT) / divT;
                int vPatch = (int)vGlobal;
                float v = vGlobal - vPatch;
                float[] vCoeff = isVCubic ? BernsteinCoeff(v) : LinearCoeff(v);

                for (int i = 0; i <= divS + 1; i++)
                {
                    float uGlobal = (float)i * (divS) / divS;
                    int uPatch = (int)uGlobal;
                    float u = uGlobal - uPatch;
                    float[] uCoeff = isUCubic ? BernsteinCoeff(u) : LinearCoeff(u);

                    var currentVertex = default(VertexInfo);
                    int uDim = isUCubic ? 4 : 2;
                    int vDim = isVCubic ? 4 : 2;

                    for (int ui = 0; ui < uDim; ui++)
                    {
                        for (int vi = 0; vi < vDim; vi++)
                        {
                            int uIndex = uPatch + ui;
                            int vIndex = vPatch + vi;
                            if (uIndex >= sp_ucount || vIndex >= sp_vcount)
                                continue;

                            PointMultAdd(ref currentVertex, ref controlPoints[uIndex, vIndex], uCoeff[ui] * vCoeff[vi]);
                        }
                    }

                    currentVertex.Texture.X = uGlobal;
                    currentVertex.Texture.Y = vGlobal;
                    splinePatch[i, j] = currentVertex;
                }
            }

            GpuProcessor.GpuBackEnd.BeforeDraw(GpuStateStructPointer);
            GpuProcessor.GpuBackEnd.DrawSpline(GlobalGpuState, GpuStateStructPointer, splinePatch, sp_ucount, sp_vcount, sp_utype, sp_vtype, normalizedUType, normalizedVType);
        }

        internal void JumpRelativeOffset(uint Address)
        {
            uint newAddress = GpuStateStructPointer.GetAddressRelativeToBaseOffset(Address);

            if (InstructionAddressStall != 0 && newAddress >= InstructionAddressStall)
            {
                //Logger.Warning($"Process {Id} 0x{InstructionAddressCurrent:X} -> 0x{newAddress:X} | Stall 0x{InstructionAddressStall:X}");
                if (PSPDrivers.GameInfo.IsIso) SetInstructionAddressStall(0);
            }
            InstructionAddressCurrent = newAddress;
        }

        internal void JumpAbsolute(uint Address)
        {
            InstructionAddressCurrent = Address;
        }

        internal void CallRelativeOffset(uint Address)
        {
            CallStack.Push(InstructionAddressCurrent);
            CallStack.Push((uint)GpuStateStructPointer.BaseOffset);
            JumpRelativeOffset(Address);
        }

        internal void Ret()
        {
            if (CallStack.Count > 0)
            {
                GpuStateStructPointer.BaseOffset = CallStack.Pop();
                JumpAbsolute(CallStack.Pop());
            }
            else
            {
                Console.Error.WriteLine("Stack is empty");
            }
        }

        public void GeListSync(Action NotifyOnceCallback)
        {
            //Thread.Sleep(200);
            //Status2.CallbackOnStateOnce(Status2Enum.Free, NotifyOnceCallback);
            Status.CallbackOnStateOnce(GEProcesStatusEnum.Completed, NotifyOnceCallback);
        }

        public void DoFinish(uint PC, uint Arg, bool ExecuteNow)
        {
            //Console.WriteLine("FINISH: Arg:{0}", Arg);

            if (Callbacks.FinishFunction != 0)
            {
                GpuProcessor.Connector.Finish(PC, Callbacks, Arg, ExecuteNow);
            }
        }

        public void DoSignal(uint PC, uint Signal, SignalBehavior Behavior, bool ExecuteNow)
        {
            //Console.WriteLine("SIGNAL : {0}: Behavior:{1}", Signal, Behavior);

            Status.SetValue(GEProcesStatusEnum.Paused);

            if (Callbacks.SignalFunction != 0)
            {
                //Console.Error.WriteLine("OP_SIGNAL! ({0}, {1})", Signal, Behavior);
                GpuProcessor.Connector.Signal(PC, Callbacks, Signal, Behavior, ExecuteNow);
            }

            Status.SetValue(GEProcesStatusEnum.Drawing);
        }

        public void SetQueued()
        {
            Status.SetValue(GEProcesStatusEnum.Queued);
        }

        public void SetDequeued()
        {
            //Status2.SetValue(Status2Enum.Dequeued);
        }

        public void SetFree()
        {
            //Status2.SetValue(Status2Enum.Free);
            Available = true;
        }

        public GEProcesStatusEnum PeekStatus()
        {
            return Status.Value;
        }

        public void DeQueue()
        {
            Done = true;
            GpuProcessor.GEProcessQueue.Remove(this);
        }
    }
}
