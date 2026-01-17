using ScePSP.Types;
using System.Runtime.InteropServices;

namespace ScePSP.GE.State.SubStates
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct ClutStateStruct
    {
        public uint Address;
        public int Shift;
        public int Mask;
        public int Start;
        public GuPixelFormats PixelFormat;
        public byte* Data;
        public int NumberOfColors;
    }
}
