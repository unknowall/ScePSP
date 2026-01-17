using ScePSP.GE.State;
using ScePSPUtils;
using System;

namespace ScePSP.GE.Run
{
    public unsafe sealed partial class GERunner
    {
        public static readonly GERunner Methods = new GERunner();

        private static readonly Logger Logger = Logger.GetLogger("GERunner");

        public GlobalGpuState GlobalGpuState;
        public GECore GECore;
        public OpCodes OpCode;
        public uint Params24;
        public uint PC;

        private GuPrimitiveType _lastPrimType, _lastPPrimType;

        private GERunner()
        {
        }

        public GERunner(GECore Ge, State.GlobalGpuState GlobalGpuState)
        {
            this.GECore = Ge;
            this.GlobalGpuState = GlobalGpuState;
        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort Param16(int Offset)
        {
            return (ushort)(Params24 >> Offset);
        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte Param8(int Offset)
        {
            return (byte)(Params24 >> Offset);
        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint Extract(int Offset, int Count)
        {
            return BitUtils.Extract(Params24, Offset, Count);
        }

        ////[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public TType Extract<TType>(int Offset, int Count)
        //{
        //	return (TType)BitUtils.Extract(Params24, Offset, Count);
        //}

        public GpuStateStruct* GpuState
        {
            get
            {
                return GECore.GEStateStruct;
            }
        }

        public float Float1
        {
            get
            {
                return MathFloat.ReinterpretUIntAsFloat(Params24 << 8);
            }
        }

        public bool Bool1
        {
            get
            {
                return Params24 != 0;
            }
        }

        public void UNIMPLEMENTED_NOTICE()
        {
            if (GECore.GeList.Config.NoticeUnimplementedGpuCommands)
            {
                Console.Error.WriteLineColored(ConsoleColor.Red, "Unimplemented GpuOpCode: {0} : {1:X}", OpCode, Params24);
            }
        }

        public void OP_UNKNOWN()
        {
            Console.WriteLine("Unhandled GpuOpCode: {0} : {1:X}", OpCode, Params24);
        }
    }
}
