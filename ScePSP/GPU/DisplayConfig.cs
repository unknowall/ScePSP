using System;

namespace ScePSP.Core.Components.Display
{
    public class DisplayConfig
    {
        public bool VerticalSynchronization = true;
        public bool Enabled = true;
        public IntPtr WindowHandle = IntPtr.Zero;
        public int Width = PspDisplay.MaxVisibleWidth * 2;
        public int Height = PspDisplay.MaxVisibleHeight * 2;
    }
}