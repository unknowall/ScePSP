using System;

namespace ScePSP.Components.Display
{
    public class DisplayConfig
    {
        public string ID;
        public string Title;

        public bool VerticalSynchronization = true;
        public bool Enabled = true;

        public int Width = 480 * 2;
        public int Height = 272 * 2;

        public bool H264Enabled = false;

        public bool Runing = false;

        public IntPtr WindowHandle;
    }
}
