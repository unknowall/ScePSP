using LightGL;
using ScePSP.BackEnd.OpenGL;
using ScePSP.Components.Display;
using ScePSP.Display;
using ScePSP.GE;
using ScePSP.Hle.Managers;
using ScePSP.Memory;
using ScePSPUtils;
using System;
using System.Diagnostics;

namespace ScePSP.Runner.Display
{
    public sealed class DisplayThread : DeviceThread
    {
        [Context]
        private HleInterruptManager HleInterruptManager;

        [Context]
        private PspDisplay PspDisplay;

        [Context]
        private DisplayConfig Config;

        [Context]
        private PspMemory Memory;

        [Context]
        private GEList GEList;

        protected override string ThreadName { get { return "DisplayThread"; } }

        IGlContext Context;
        GLRectangleF TextureRect;
        GLBuffer VertexBuffer;
        GLBuffer TexCoordsBuffer;
        GLShader Shader;
        GLTexture2D TexVram;
        bool Vflip;
        public bool FullSpeed = false;

        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        private int _frameCount;
        public float CurrentFPS { get; private set; }

        public class ShaderInfoClass
        {
            public GlAttribute position;
            public GlAttribute texCoords;
            public GlUniform texture;
        }
        static ShaderInfoClass ShaderInfo = new ShaderInfoClass();

        public DisplayThread()
        {
        }

        bool Inited = false;

        public void InitRender()
        {
            if (Inited) return;

            if (Config.WindowHandle != IntPtr.Zero)
            {
                Context = GlContextFactory.CreateFromWindowHandle(Config.WindowHandle);
                Context.MakeCurrent();

                Shader = new GLShader(
                    "attribute vec4 position; attribute vec4 texCoords; varying vec2 v_texCoord; void main() { gl_Position = position; v_texCoord = texCoords.xy; }",
                    "uniform sampler2D texture; varying vec2 v_texCoord; void main() { gl_FragColor = texture2D(texture, v_texCoord); }"
                    );

                Shader.BindUniformsAndAttributes(ShaderInfo);

                TextureRect = GLRectangleF.FromCoords(0, 0, 1, 1);
                TexCoordsBuffer = GLBuffer.Create().SetData(TextureRect.GetFloat2TriangleStripCoords());
                VertexBuffer = GLBuffer.Create().SetData(GLRectangleF.FromCoords(-1, -1, +1, +1).GetFloat2TriangleStripCoords());

                ShaderInfo.position.SetData<float>(VertexBuffer, 2);
                ShaderInfo.texCoords.SetData<float>(TexCoordsBuffer, 2);
                ShaderInfo.texture.Set(0);
                Shader.Use();

                GL.BindFramebuffer(GL.GL_FRAMEBUFFER, 0);
                Context.ReleaseCurrent();
                PspDisplay.DrawEvent += RenderToWindow;
                Inited = false;
            }
        }

        public void free()
        {
            if (Context == null) return;

            PspDisplay.DrawEvent -= RenderToWindow;

            TexVram?.Dispose();
            VertexBuffer.Dispose();
            TexCoordsBuffer.Dispose();
            Shader.Dispose();
            Context.ReleaseCurrent();
            Context.Dispose();
        }

        private unsafe void GetFromRam()
        {
            if (TexVram == null)
            {
                var width = PspDisplay.CurrentInfo.Width;
                var height = PspDisplay.CurrentInfo.Height;
                TexVram = GLTexture2D.Create().SetFormat(TextureFormat.RGBA).SetSize(width, height);
            }
            TexVram.Bind();
            var pixels2 = new uint[PspDisplay.MaxBufferArea];
            var displayData = Memory.Range<uint>(PspDisplay.CurrentInfo.FrameAddress, PspDisplay.MaxBufferArea);
            for (var m = 0; m < PspDisplay.MaxBufferArea; m++)
            {
                var color = displayData[m];
                uint r = BitUtils.Extract(color, 0, 8);
                uint g = BitUtils.Extract(color, 8, 8);
                uint b = BitUtils.Extract(color, 16, 8);
                //pixels2[m] = (b << 24) | (g << 16) | (r << 8) | 0xFF;
                pixels2[m] = ((uint)0xFF << 24) | (b << 16) | (g << 8) | r;
            }
            fixed (uint* pp = pixels2)
            {
                GL.TexImage2D(GL.GL_TEXTURE_2D, 0, GL.GL_RGBA, 512, 272, 0, GL.GL_RGBA, GL.GL_UNSIGNED_BYTE, pp);
            }
        }

        public void UpdateFrame()
        {
            _frameCount++;
            double elapsedSeconds = _stopwatch.Elapsed.TotalSeconds;
            if (elapsedSeconds >= 1.0f)
            {
                CurrentFPS = (float)(_frameCount / elapsedSeconds);
                _frameCount = 0;
                _stopwatch.Restart();
            }
        }

        public unsafe void RenderToWindow()
        {
            var GLE = GEList.BackEnd as GLBackEnd;

            if (Context == null) return;

            Context.MakeCurrent();

            if (!PspDisplay.CurrentInfo.PlayingVideo && GEList.UsingGe && GLE.GeContext != null)
            {
                GLE.Frames.TryGetValue(PspDisplay.CurrentInfo.FrameAddress, out var FB);
                if (FB == null) { UpdateFrame(); return; }
                FB.TextureColor.Bind();
                //GLE.CurrentFB.TextureColor.Bind();
                if (Vflip)
                {
                    TexCoordsBuffer.SetData(TextureRect.GetFloat2TriangleStripCoords());
                    Vflip = false;
                }
            }
            else
            {
                GetFromRam();

                if (!Vflip)
                {
                    TexCoordsBuffer.SetData(TextureRect.VFlip().GetFloat2TriangleStripCoords());
                    Vflip = true;
                }
            }

            GL.Viewport(0, 0, Config.Width, Config.Height);
            GL.ClearColor(0, 0, 0, 1);
            GL.Clear(GL.GL_COLOR_BUFFER_BIT);

            GL.DrawArrays(GL.GL_TRIANGLE_STRIP, 0, 4);

            Context.SwapBuffers();

            UpdateFrame();

            //Console.WriteLine($" Current GETexture: {GLE.GeTexture.Count}");

            //GLE.FrameFB.Clear();
        }

        protected override void Main()
        {
            var VSyncTimeIncrement = TimeSpan.FromSeconds(1.0 / (PspDisplay.HorizontalSyncHertz / (double)(PspDisplay.VsyncRow)));
            //var VSyncTimeIncrement = TimeSpan.FromSeconds(1.0 / (PspDisplay.HorizontalSyncHertz / (double)(PspDisplay.VsyncRow / 2)));
            var EndTimeIncrement = TimeSpan.FromSeconds(1.0 / (PspDisplay.HorizontalSyncHertz / (double)(PspDisplay.NumberOfRows)));
            var VBlankInterruptHandler = HleInterruptManager.GetInterruptHandler(PspInterrupts.PSP_VBLANK_INT);

            while (true)
            {
                var StartTime = DateTime.UtcNow;
                var VSyncTime = StartTime + VSyncTimeIncrement;
                var EndTime = StartTime + EndTimeIncrement;

                ThreadTaskQueue.HandleEnqueued();

                if (!Running) break;

                // Draw time
                PspDisplay.TriggerDrawStart();
                if (!FullSpeed) ThreadUtils.SleepUntilUtc(VSyncTime);

                // VBlank time
                PspDisplay.TriggerVBlankStart();
                VBlankInterruptHandler.Trigger();
                if (!FullSpeed) ThreadUtils.SleepUntilUtc(EndTime);
                //if (!FullSpeed) System.Threading.Thread.Sleep(14);
                PspDisplay.TriggerVBlankEnd();
            }

            free();
        }
    }
}
