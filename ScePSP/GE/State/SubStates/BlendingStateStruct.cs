using ScePSP.Types;
using System.Runtime.InteropServices;

namespace ScePSP.GE.State.SubStates
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BlendingStateStruct
    {
        public bool Enabled;

        public BlendingOpEnum Equation;

        public GuBlendingFactorSource FunctionSource;

        public GuBlendingFactorDestination FunctionDestination;

        public ColorfStruct FixColorSource;

        public ColorfStruct FixColorDestination;

        public OutputPixel ColorMask;
    }
}
