using cscodec.util;
using ScePSP;
using ScePSP.Core;
using ScePSP.Core.Components.Controller;
using ScePSP.Core.Components.Display;
using ScePSP.Core.Components.Rtc;
using ScePSP.Core.Gpu;
using ScePSP.Core.Gpu.Impl.Opengl;
using ScePSP.Core.Memory;
using ScePSP.Core.Types.Controller;
using ScePSP.Runner;
using ScePSP.Runner.Components.Display;
using ScePSPPlatform.GL;
using ScePSPPlatform.GL.Utils;
using ScePSPUtils;
using SDL2;
using System;
using System.Diagnostics;
using System.Windows.Forms;
using static ScePSPUtils.Logger;

#pragma warning disable CS0436
#pragma warning disable CS8602
#pragma warning disable CS0649

class Program
{
    static IntPtr window;
    static IGlContext? Context;

    static InjectContext? injector;
    static PspRtc? rtc;
    static PspDisplay? display;
    static DisplayComponentThread? displayComponent;
    static PspMemory? memory;
    static PspController? controller;
    static GpuProcessor? gpu;
    static PspEmulator? pspEmulator;

    static SceCtrlData ctrlData;
    static int lx, ly;
    static int pressingAnalogLeft, pressingAnalogRight, pressingAnalogUp, pressingAnalogDown;

    static GLShader? Shader;
    static GLBuffer? VertexBuffer;
    static GLBuffer? TexCoordsBuffer;
    static GLTexture? TexVram;
    static GLTexture? DrawTexture;
    //static GLTexture? DrawDepth;
    static bool TextureVerticalFlip;
    //static GLTexture? TestTexture;
    public class ShaderInfoClass
    {
        public GlAttribute? position;
        public GlAttribute? texCoords;
        public GlUniform? texture;
    }
    static ShaderInfoClass ShaderInfo = new ShaderInfoClass();

    [STAThreadAttribute]
    private static void Main(string[] args)
    {
        if (SDL.SDL_Init(SDL.SDL_INIT_VIDEO | SDL.SDL_INIT_AUDIO) != 0)
        {
            Console.Error.WriteLine("Couldn't initialize SDL");
            return;
        }

        window = SDL.SDL_CreateWindow(
            "ScePSP",
            SDL.SDL_WINDOWPOS_CENTERED, SDL.SDL_WINDOWPOS_CENTERED,
            PspDisplay.MaxVisibleWidth * 2, PspDisplay.MaxVisibleHeight * 2,
            SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN | SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE
        );

        SDL.SDL_SysWMinfo wmInfo = new SDL.SDL_SysWMinfo();
        SDL.SDL_VERSION(out wmInfo.version);
        SDL.SDL_GetWindowWMInfo(window, ref wmInfo);
        IntPtr windowhwnd = wmInfo.info.win.window;

    LoadRom:
        OpenFileDialog ofn = new OpenFileDialog();
        ofn.Filter = "PSP Roms (*.pbp, *.prx, *.iso, *.elf, *.zip)|*.pbp;*.prx;*.iso;*.elf;*.zip";
        ofn.Title = "PSP Rom";
        if (ofn.ShowDialog() == DialogResult.Cancel) goto LoadRom;

        Context = GlContextFactory.CreateFromWindowHandle(windowhwnd);
        Context.MakeCurrent();
        Shader = new GLShader(
            "attribute vec4 position; attribute vec4 texCoords; varying vec2 v_texCoord; void main() { gl_Position = position; v_texCoord = texCoords.xy; }",
            "uniform sampler2D texture; varying vec2 v_texCoord; void main() { gl_FragColor = texture2D(texture, v_texCoord); }"
        );
        VertexBuffer = GLBuffer.Create().SetData(ScePSPPlatform.RectangleF.FromCoords(-1, -1, +1, +1).GetFloat2TriangleStripCoords());
        Shader.BindUniformsAndAttributes(ShaderInfo);
        //TestTexture = GLTexture.Create().SetFormat(TextureFormat.RGBA).SetSize(2, 2).SetData(new uint[] { 0xFF0000FF, 0xFF00FFFF, 0xFFFF00FF, 0xFFFFFFFF });
        Context.ReleaseCurrent();

        //Logger.OnGlobalLog += Log;

        injector = PspInjectContext.CreateInjectContext(PspStoredConfig.Load(), PspGpuType.OpenGL, PspAudioType.SDL);

        pspEmulator = injector.GetInstance<PspEmulator>();

        rtc = pspEmulator.InjectContext.GetInstance<PspRtc>();
        display = pspEmulator.InjectContext.GetInstance<PspDisplay>();
        displayComponent = pspEmulator.InjectContext.GetInstance<DisplayComponentThread>();
        memory = pspEmulator.InjectContext.GetInstance<PspMemory>();
        controller = pspEmulator.InjectContext.GetInstance<PspController>();
        gpu = pspEmulator.InjectContext.GetInstance<GpuProcessor>();

        PspDisplay.DrawEvent += DrawFrame;

        ctrlData = new SceCtrlData { Buttons = 0, Lx = 0, Ly = 0 };
        lx = 0;
        ly = 0;
        pressingAnalogLeft = 0;
        pressingAnalogRight = 0;
        pressingAnalogUp = 0;
        pressingAnalogDown = 0;

        pspEmulator.StartAndLoad(ofn.FileName, RunMainLoop, false, false);
    }

    private static void Log(string name, Level level, string message, StackFrame stack)
    {
        switch (level)
        {
            case Level.Fatal:
            case Level.Warning:
            case Level.Error:
                Console.WriteLine($"[{level}] {name}: {message}");
                break;
        }
    }

    private static void GetTextureFromGpu()
    {
        var OpengImpl = (gpu.GpuImpl as OpenglGpuImpl);
        OpengImpl.RenderbufferManager.GetDrawBufferTextureAndLock(
            new DrawBufferKey() { Address = display.CurrentInfo.FrameAddress },
            (DrawBuffer) =>
            {
                if (DrawBuffer != null)
                {
                    var RenderTarget = DrawBuffer.RenderTarget;
                    if (GL.glIsTexture(RenderTarget.TextureColor.Texture))
                    {
                        TextureVerticalFlip = true;
                        DrawTexture = RenderTarget.TextureColor;
                        //DrawDepth = RenderTarget.TextureDepth;
                        return;
                    }
                    else
                    {
                        GetTextureFromRam();
                    }
                }
            });
    }

    private unsafe static void GetTextureFromRam()
    {
        if (TexVram == null)
        {
            TexVram = GLTexture.Create().SetFormat(TextureFormat.RGBA).SetSize(1, 1);
        }

        TexVram.Bind();

        var width = display.CurrentInfo.Width;
        var height = display.CurrentInfo.Height;
        var pixels2 = new uint[PspDisplay.MaxBufferArea];
        var displayData = memory.Range<uint>(display.CurrentInfo.FrameAddress, PspDisplay.MaxBufferArea);
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
            GL.glTexImage2D(GL.GL_TEXTURE_2D, 0, GL.GL_RGBA, 512, 272, 0, GL.GL_RGBA, GL.GL_UNSIGNED_BYTE, pp);
        }

        DrawTexture = TexVram;

        TextureVerticalFlip = true;
    }

    private static void DrawFrame()
    {
        lx = pressingAnalogLeft != 0 ? -pressingAnalogLeft : pressingAnalogRight;
        ly = pressingAnalogUp != 0 ? -pressingAnalogUp : pressingAnalogDown;

        ctrlData.X = lx / 3f;
        ctrlData.Y = ly / 3f;

        ctrlData.TimeStamp = (uint)rtc.UnixTimeStampTS.Milliseconds;

        controller.InsertSceCtrlData(ctrlData);

        Context.MakeCurrent();

        if (!display.CurrentInfo.PlayingVideo && gpu.UsingGe)
        {
            GetTextureFromGpu();
        }
        else
        {
            GetTextureFromRam();
        }

        GL.glViewport(0, 0, PspDisplay.MaxVisibleWidth * 2, PspDisplay.MaxVisibleHeight * 2);
        GL.glClearColor(0, 0, 0, 1);
        GL.glClear(GL.GL_COLOR_BUFFER_BIT);

        Shader.Draw(GLGeometry.GL_TRIANGLE_STRIP, 4, () =>
        {
            var TextureRect = ScePSPPlatform.RectangleF.FromCoords(0, 0, (float)display.CurrentInfo.Width / 512f, (float)display.CurrentInfo.Height / 272f);
            if (TextureVerticalFlip) TextureRect = TextureRect.VFlip();
            TexCoordsBuffer = GLBuffer.Create().SetData(TextureRect.GetFloat2TriangleStripCoords());
            ShaderInfo.texture.Set(GLTextureUnit.CreateAtIndex(0).SetFiltering(GLScaleFilter.Nearest).SetWrap(GLWrap.ClampToEdge).SetTexture(DrawTexture));
            //ShaderInfo.texture.Set(GLTextureUnit.CreateAtIndex(0).SetFiltering(GLScaleFilter.Nearest).SetWrap(GLWrap.ClampToEdge).SetTexture(DrawDepth));
            ShaderInfo.position.SetData<float>(VertexBuffer, 2);
            ShaderInfo.texCoords.SetData<float>(TexCoordsBuffer, 2);
        });

        Context.SwapBuffers();

        //Context.ReleaseCurrent();
    }

    private static void RunMainLoop(PspEmulator emulator)
    {
        var running = true;

        Console.WriteLine("Starting main loop");

        PspCtrlButtons UpdatePressing(ref int value, bool pressing)
        {
            if (pressing)
            {
                value++;
            }
            else
            {
                value = 0;
            }

            return 0;
        }

        while (running)
        {
            while (SDL.SDL_PollEvent(out var e) != 0)
            {
                switch (e.type)
                {
                    case SDL.SDL_EventType.SDL_QUIT:
                        running = false;
                        break;
                    case SDL.SDL_EventType.SDL_KEYDOWN:
                    case SDL.SDL_EventType.SDL_KEYUP:
                        var pressed = e.type == SDL.SDL_EventType.SDL_KEYDOWN;
                        PspCtrlButtons buttonMask;
                        switch (e.key.keysym.sym)
                        {
                            case SDL.SDL_Keycode.SDLK_a:
                                buttonMask = PspCtrlButtons.Square;
                                break;
                            case SDL.SDL_Keycode.SDLK_w:
                                buttonMask = PspCtrlButtons.Triangle;
                                break;
                            case SDL.SDL_Keycode.SDLK_d:
                                buttonMask = PspCtrlButtons.Circle;
                                break;
                            case SDL.SDL_Keycode.SDLK_s:
                                buttonMask = PspCtrlButtons.Cross;
                                break;
                            case SDL.SDL_Keycode.SDLK_SPACE:
                                buttonMask = PspCtrlButtons.Select;
                                break;
                            case SDL.SDL_Keycode.SDLK_RETURN:
                                buttonMask = PspCtrlButtons.Start;
                                break;
                            case SDL.SDL_Keycode.SDLK_UP:
                                buttonMask = PspCtrlButtons.Up;
                                break;
                            case SDL.SDL_Keycode.SDLK_DOWN:
                                buttonMask = PspCtrlButtons.Down;
                                break;
                            case SDL.SDL_Keycode.SDLK_LEFT:
                                buttonMask = PspCtrlButtons.Left;
                                break;
                            case SDL.SDL_Keycode.SDLK_RIGHT:
                                buttonMask = PspCtrlButtons.Right;
                                break;
                            case SDL.SDL_Keycode.SDLK_i:
                                buttonMask = UpdatePressing(ref pressingAnalogUp, pressed);
                                break;
                            case SDL.SDL_Keycode.SDLK_k:
                                buttonMask = UpdatePressing(ref pressingAnalogDown, pressed);
                                break;
                            case SDL.SDL_Keycode.SDLK_j:
                                buttonMask = UpdatePressing(ref pressingAnalogLeft, pressed);
                                break;
                            case SDL.SDL_Keycode.SDLK_l:
                                buttonMask = UpdatePressing(ref pressingAnalogRight, pressed);
                                break;
                            default:
                                buttonMask = 0;
                                break;
                        }
                        ;

                        if (pressed)
                        {
                            ctrlData.Buttons |= buttonMask;
                        }
                        else
                        {
                            ctrlData.Buttons &= ~buttonMask;
                        }

                        break;
                }
            }
        }

    }

}