using System;
using ScePSPPlatform.GL.Impl.Android;
using ScePSPPlatform.GL.Impl.Linux;
using ScePSPPlatform.GL.Impl.Mac;
using ScePSPPlatform.GL.Impl.Windows;
using ScePSP.Core;

namespace ScePSPPlatform.GL
{
    public class GlContextFactory
    {
        [ThreadStatic] public static IGlContext Current;

        public static IGlContext CreateWindowless() => CreateFromWindowHandle(IntPtr.Zero);

        public static IGlContext CreateFromWindowHandle(IntPtr windowHandle) =>
            Platform.OS switch
            {
                OS.Windows => WinGlContext.FromWindowHandle(windowHandle),
                OS.Mac => MacGLContext.FromWindowHandle(windowHandle),
                OS.Linux => LinuxGlContext.FromWindowHandle(windowHandle),
                OS.Android => AndroidGLContext.FromWindowHandle(windowHandle),
                _ => throw new NotImplementedException($"Not implemented OS: {Platform.OS}")
            };
    }
}