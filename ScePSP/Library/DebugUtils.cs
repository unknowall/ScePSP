using System.Diagnostics;

namespace ScePSP.Core
{
    public static class DebugUtils
    {
        public static void IsDebuggerPresentDebugBreak()
        {
            if (Debugger.IsAttached) Debugger.Break();
        }
    }
}