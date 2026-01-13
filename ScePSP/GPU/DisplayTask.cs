using LightGL;
using ScePSP.Core.Components.Display;
using ScePSP.Core.GpuBackEnd.OpenGL;
using ScePSP.Hle.Managers;
using ScePSP.Utils;
using ScePSPUtils;
using System;
using System.Diagnostics;
using System.Threading;

namespace ScePSP.Runner.Tasks.Display
{
    public sealed class DisplayTask : PspMainTask
    {
        protected override string ThreadName => "DisplayTask";

        private HleInterruptManager _hleInterruptManager => PSPDrivers.HLE.HleInterruptManager;

        private PspDisplay _pspDisplay => PSPDrivers.PspDisplay;

        TimeSpan vSyncTimeIncrement = TimeSpan.FromSeconds(1.0 / (PspDisplay.HorizontalSyncHertz / (double)PspDisplay.VsyncRow));

        // HACK to give more time to render!
        //var VSyncTimeIncrement = TimeSpan.FromSeconds(1.0 / (PspDisplay.HorizontalSyncHertz / (double)(PspDisplay.VsyncRow / 2))); 

        TimeSpan endTimeIncrement = TimeSpan.FromSeconds(1.0 / (PspDisplay.HorizontalSyncHertz / (double)PspDisplay.NumberOfRows));

        HleInterruptHandler vBlankInterruptHandler;

        IGlContext Context;

        GLRectangleF TextureRect;
        GLBuffer VertexBuffer;
        GLBuffer TexCoordsBuffer;
        GLShader Shader;
        GLTexture2D TexVram;
        bool Vflip;

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

        public bool triggerStuff = true;

        public DisplayTask()
        {
            if (PSPDrivers.Config.DisplayConfig.WindowHandle != IntPtr.Zero)
            {
                Context = GlContextFactory.CreateFromWindowHandle(PSPDrivers.Config.DisplayConfig.WindowHandle);

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

                _pspDisplay.VBlankEventCall += RenderToWindow;
            }
        }

        private unsafe void GetFromRam()
        {
            if (TexVram == null)
            {
                var width = PSPDrivers.Devices.Display.CurrentInfo.Width;
                var height = PSPDrivers.Devices.Display.CurrentInfo.Height;
                TexVram = GLTexture2D.Create().SetFormat(TextureFormat.RGBA).SetSize(width, height);
            }
            TexVram.Bind();
            var pixels2 = new uint[PspDisplay.MaxBufferArea];
            var displayData = PSPDrivers.PspMemory.Range<uint>(PSPDrivers.Devices.Display.CurrentInfo.FrameAddress, PspDisplay.MaxBufferArea);
            for (var m = 0; m < PspDisplay.MaxBufferArea; m++)
            {
                var color = displayData[m];
                uint r = color.Extract(0, 8);
                uint g = color.Extract(8, 8);
                uint b = color.Extract(16, 8);
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

        public void RenderToWindow()
        {
            var GLE = PSPDrivers.GpuBackEnd as OpenglBackEnd;

            if (Context == null || GLE.GpuState == null) return;

            Context.MakeCurrent();

            if (!PSPDrivers.Devices.Display.CurrentInfo.PlayingVideo && PSPDrivers.GE.UsingGe)
            {
                GLE.FrameFB.TextureColor.Bind();

                //if (GLE.GeTexture.TryGetValue(GLE.GpuState.DrawBufferState.Address, out var Texture))
                //{
                //    Texture.Bind();
                //}

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

            GL.Viewport(0, 0, PSPDrivers.Config.DisplayConfig.Width, PSPDrivers.Config.DisplayConfig.Height);
            GL.ClearColor(0, 0, 0, 1);
            GL.Clear(GL.GL_COLOR_BUFFER_BIT);

            GL.DrawArrays(GL.GL_TRIANGLE_STRIP, 0, 4);

            Context.SwapBuffers();

            UpdateFrame();

            //Console.WriteLine($" Current GETexture: {GLE.GeTexture.Count}");

            //GLE.FrameFB.Clear();
        }

        public void Step(Action DrawStart, Action VBlankStart, Action VBlankEnd)
        {
            var startTime = DateTime.UtcNow;
            var vSyncTime = startTime + vSyncTimeIncrement;
            var endTime = startTime + endTimeIncrement;

            ThreadTaskQueue.HandleEnqueued();

            if (!Running) return;

            // Draw time
            DrawStart();
            //ThreadUtils.SleepUntilUtc(vSyncTime);

            // VBlank time
            VBlankStart();
            vBlankInterruptHandler.Trigger();
            ThreadUtils.SleepUntilUtc(endTime);
            VBlankEnd();
        }

        protected override void Main()
        {
            var threadId = Environment.CurrentManagedThreadId;

            Console.Out.WriteLineColored(ConsoleColor.White, $"## DISPLAY Runing ThreadId={threadId}");

            vBlankInterruptHandler = _hleInterruptManager.GetInterruptHandler(PspInterrupts.PspVblankInt);

            while (Running)
            {
                if (triggerStuff)
                {
                    Step(_pspDisplay.TriggerDrawStart, _pspDisplay.TriggerVBlankStart, _pspDisplay.TriggerVBlankEnd);
                }
                else
                {
                    Thread.Sleep(16.Milliseconds());
                }
            }
        }
    }
}