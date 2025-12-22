using System;
using ScePSPPlatform.GL.Impl.Windows;

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