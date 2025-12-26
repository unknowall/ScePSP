using System.Runtime.InteropServices;

namespace ScePSP.Core.Gpu.State
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct GpuRectStruct
    {
        public short Left;
        public short Top;
        public short Right;
        public short Bottom;

        public int Width => Right - Left;
        public int Height => Bottom - Top;

        public bool IsFull => Left <= 0 && Top <= 0 && Right >= 480 && Bottom >= 272;

        public GpuRectStruct(short left, short top, short right, short bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }
    }
}