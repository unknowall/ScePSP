using System.Runtime.InteropServices;

namespace ScePSP.GE.State.SubStates
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ClipPlaneStateStruct
    {
        /// <summary>
        /// Clip Plane Enable (GL_CLIP_PLANE0)
        /// </summary>
        public bool Enabled;

        public GpuRectStruct Scissor;
    }
}
