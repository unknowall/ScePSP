using ScePSPPlatform.GL.Impl.Windows;
using System;

namespace ScePSPPlatform.GL
{
    public interface IGlContext : IDisposable
    {
        GlContextSize Size { get; }
        IGlContext MakeCurrent();
        IGlContext ReleaseCurrent();
        IGlContext SwapBuffers();
    }
}