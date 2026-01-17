using ScePSP.Types;
using System.Runtime.InteropServices;

namespace ScePSP.GE.State.SubStates
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BlendingStateStruct
    {
        /// <summary>
        /// 
        /// </summary>
        public bool Enabled;

        /// <summary>
        /// 
        /// </summary>
        public BlendingOpEnum Equation;

        /// <summary>
        /// 
        /// </summary>
        public GuBlendingFactorSource FunctionSource;

        /// <summary>
        /// 
        /// </summary>
        public GuBlendingFactorDestination FunctionDestination;

        /// <summary>
        /// 
        /// </summary>
        public ColorfStruct FixColorSource;

        /// <summary>
        /// 
        /// </summary>
        public ColorfStruct FixColorDestination;

        /// <summary>
        /// 
        /// </summary>
        public OutputPixel ColorMask;
    }
}
