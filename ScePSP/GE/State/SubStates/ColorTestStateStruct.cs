using ScePSP.Types;
using System.Runtime.InteropServices;

namespace ScePSP.GE.State.SubStates
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ColorTestStateStruct
    {
        /// <summary>
        /// 
        /// </summary>
        public bool Enabled;

        /// <summary>
        /// 
        /// </summary>
        public OutputPixel Ref;

        /// <summary>
        /// 
        /// </summary>
        public OutputPixel Mask;

        /// <summary>
        /// 
        /// </summary>
        public ColorTestFunctionEnum Function;
    }
}
