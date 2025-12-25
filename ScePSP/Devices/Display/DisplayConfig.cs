using System;

namespace ScePSP.Core.Components.Display
{
    public class DisplayConfig
    {
        public bool VerticalSynchronization = true;
        public bool Enabled = true;
        public IntPtr WindowHandle = IntPtr.Zero;
    }
}