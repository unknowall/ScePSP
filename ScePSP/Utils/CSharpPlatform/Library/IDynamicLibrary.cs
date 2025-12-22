using System;

namespace ScePSPPlatform.Library
{
    public interface IDynamicLibrary : IDisposable
    {
        IntPtr GetMethod(string Name);
    }
}