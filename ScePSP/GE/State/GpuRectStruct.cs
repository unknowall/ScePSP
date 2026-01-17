using System.Runtime.InteropServices;
namespace ScePSP.GE.State
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

        public bool IsFull
        {
            get
            {
                return (Left <= 0 && Top <= 0) && (Right >= 480 && Bottom >= 272);
            }
        }
    }
}
