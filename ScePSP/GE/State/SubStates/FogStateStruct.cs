using System.Runtime.InteropServices;

namespace ScePSP.GE.State.SubStates
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct FogStateStruct
    {
        /// <summary>
        /// FOG Enable (GL_FOG)
        /// </summary>
        public bool Enabled;

        public ColorfStruct Color;

        public float Dist;

        public float End;

        /// <summary>
        /// Default Value: 0.1
        /// </summary>
        public float Density;

        public int Mode;

        public int Hint;
    }
}
