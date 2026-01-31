using ScePSP.Memory;
using ScePSP.Rtc;
using ScePSP.Threading.Synchronization;
using ScePSP.Types;
using ScePSP.Utils;
using System;
using System.Drawing;

namespace ScePSP.Display
{
    public class PspDisplay
    {
        public bool IsVblank { get; protected set; }

        public const double ProcessedPixelsPerSecond = 9000000.0;
        public const double CyclesPerPixel = 1.0;
        public const double PixelsInARow = 525.0;
        public const double VsyncRow = 272.0;
        public const double NumberOfRows = 286.0;
        public const float hCountPerVblank = 285.72f;
        public const double HorizontalSyncHertz = 17142.857142857141;
        public const double VerticalSyncHertz = 59.940059940059932;

        [Context]
        private PspRtc PspRtc;

        [Context]
        private PspMemory Memory;

        public PspDisplay.Info CurrentInfo = new PspDisplay.Info
        {
            Enabled = true,
            FrameAddress = 67108864u,
            BufferWidth = 512,
            PixelFormat = GuPixelFormats.RGBA_8888,
            Mode = 0,
            Width = 480,
            Height = 272
        };

        private DateTime StartDrawTime;
        public PspWaitEvent VBlankEvent = new PspWaitEvent();
        private int _VblankCount;

        public enum SyncMode
        {
            Immediate,
            NextFrame
        }

        public struct Info
        {
            public int BufferWidthHeightCount
            {
                get
                {
                    return this.BufferWidth * this.Height;
                }
            }
            public bool Enabled;
            public bool PlayingVideo;
            public uint FrameAddress;
            public int BufferWidth;
            public GuPixelFormats PixelFormat;
            public int Mode;
            public int Width;
            public int Height;
        }

        public const int MaxBufferWidth = 512;
        public const int MaxBufferHeight = 272;
        public const int MaxBufferArea = MaxBufferWidth * MaxBufferHeight;
        public const int MaxVisibleWidth = 480;
        public const int MaxVisibleHeight = 272;
        public const int MaxVisibleArea = MaxVisibleWidth * MaxVisibleHeight;

        private PspDisplay()
        {
        }

        public static event Action DrawEvent;

        public void TriggerDrawStart()
        {
            this.StartDrawTime = DateTime.UtcNow;
            if (PspDisplay.DrawEvent != null)
            {
                PspDisplay.DrawEvent();
            }
        }

        public int GetHCount()
        {
            return (int)((DateTime.UtcNow - this.StartDrawTime).TotalSeconds / 5.833333333333334E-05);
        }

        public static event Action VBlankCallback;

        public void VBlankCallbackOnce(Action Callback)
        {
            Action Callback2 = null;
            Callback2 = delegate ()
            {
                PspDisplay.VBlankCallback -= Callback2;
                Callback();
            };
            PspDisplay.VBlankCallback += Callback2;
        }

        public void TriggerVBlankStart()
        {
            if (PspDisplay.VBlankCallback != null)
            {
                PspDisplay.VBlankCallback();
            }
            this.VBlankEvent.Signal();
            if (this.VBlankEventCall != null)
            {
                this.VBlankEventCall();
            }
            this.VblankCount++;
            this.IsVblank = true;
        }

        public void TriggerVBlankEnd()
        {
            this.IsVblank = false;
        }

        public event Action VBlankEventCall;

        public int VblankCount
        {
            get
            {
                return this._VblankCount;
            }
            set
            {
                this._VblankCount = value;
            }
        }

        public unsafe Bitmap TakeScreenshot()
        {
            return new PspBitmap(this.CurrentInfo.PixelFormat, this.CurrentInfo.BufferWidth, this.CurrentInfo.Height,
                (byte*)this.Memory.PspAddressToPointerSafe(this.CurrentInfo.FrameAddress,
                PixelFormatDecoder.GetPixelsSize(this.CurrentInfo.PixelFormat, this.CurrentInfo.BufferWidth * this.CurrentInfo.Height), true, false), -1).ToBitmap();
        }
    }
}
