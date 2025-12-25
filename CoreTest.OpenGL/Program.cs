using ScePSP;
using ScePSP.Core;
using ScePSP.Core.Audio;
using ScePSP.Core.Audio.Impl.Null;
using ScePSP.Core.Components.Controller;
using ScePSP.Core.Components.Display;
using ScePSP.Core.Components.Rtc;
using ScePSP.Core.Gpu;
using ScePSP.Core.Gpu.Impl.Opengl;
using ScePSP.Core.Gpu.Impl.Opengl.Modules;
using ScePSP.Core.Memory;
using ScePSP.Core.Types.Controller;
using ScePSP.Hle.Modules.emulator;
using ScePSP.Runner;
using ScePSP.Runner.Components.Display;
using ScePSP.Utils.Utils;
using ScePSPPlatform.GL;
using ScePSPPlatform.GL.Impl.Windows;
using ScePSPPlatform.GL.Utils;
using ScePSPUtils;
using SDL2;
using System;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

#pragma warning disable CS0436

class Program
{
    static IntPtr window;
    static IntPtr renderer;
    static IntPtr texture;

    [STAThreadAttribute]
    static unsafe void Main(string[] args)
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
            SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN | SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE | SDL.SDL_WindowFlags.SDL_WINDOW_OPENGL
        );

        renderer = SDL.SDL_CreateRenderer(window, -1, SDL.SDL_RendererFlags.SDL_RENDERER_PRESENTVSYNC | SDL.SDL_RendererFlags.SDL_RENDERER_ACCELERATED);

        texture = SDL.SDL_CreateTexture(renderer, SDL.SDL_PIXELFORMAT_RGBA8888,
            (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_TARGET, PspDisplay.MaxVisibleWidth,
            PspDisplay.MaxVisibleHeight);

    //SDL.SDL_SysWMinfo wmInfo = new SDL.SDL_SysWMinfo();
    //SDL.SDL_VERSION(out wmInfo.version);
    //SDL.SDL_GetWindowWMInfo(window, ref wmInfo);
    //IntPtr windowhwnd = wmInfo.info.win.window;

    LoadRom:
        OpenFileDialog ofn = new OpenFileDialog();
        ofn.Filter = "PSP Roms (*.pbp, *.prx, *.iso, *.elf, *.zip)|*.pbp;*.prx;*.iso;*.elf;*.zip";
        ofn.Title = "PSP Rom";
        if (ofn.ShowDialog() == DialogResult.Cancel) goto LoadRom;

        var injector = PspInjectContext.CreateInjectContext(PspStoredConfig.Load(), PspGpuType.OpenGL, PspAudioType.SDL);

        using var pspEmulator = injector.GetInstance<PspEmulator>();

        pspEmulator.StartAndLoad(ofn.FileName, RunMainLoop, false, false);
    }

    private static unsafe void RunMainLoop(PspEmulator emulator)
    {
        var running = true;

        var rtc = emulator.InjectContext.GetInstance<PspRtc>();
        var display = emulator.InjectContext.GetInstance<PspDisplay>();
        var displayComponent = emulator.InjectContext.GetInstance<DisplayComponentThread>();
        var memory = emulator.InjectContext.GetInstance<PspMemory>();
        var controller = emulator.InjectContext.GetInstance<PspController>();
        var gpu = emulator.InjectContext.GetInstance<GpuProcessor>();

        displayComponent.triggerStuff = false;

        Console.WriteLine("Starting main loop");

        var ctrlData = new SceCtrlData { Buttons = 0, Lx = 0, Ly = 0 };

        var lx = 0;
        var ly = 0;

        var pressingAnalogLeft = 0;
        var pressingAnalogRight = 0;
        var pressingAnalogUp = 0;
        var pressingAnalogDown = 0;

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

            //GLTexture Color;
            //GLTexture Depth;
            byte[] ColorPixels;

            displayComponent.Step(
                        DrawStart: () => { display.TriggerDrawStart(); },

                        VBlankStart: () => { display.TriggerVBlankStart(); },

                        VBlankEnd: () =>
                        {
                            lx = pressingAnalogLeft != 0 ? -pressingAnalogLeft : pressingAnalogRight;
                            ly = pressingAnalogUp != 0 ? -pressingAnalogUp : pressingAnalogDown;

                            ctrlData.X = lx / 3f;
                            ctrlData.Y = ly / 3f;
                            ctrlData.TimeStamp = (uint)rtc.UnixTimeStampTS.Milliseconds;

                            controller.InsertSceCtrlData(ctrlData);

                            var OpenglGpuImpl = (gpu.GpuImpl as OpenglGpuImpl);

                            if (OpenglGpuImpl.RenderbufferManager.CurrentDrawBuffer != null)
                            {
                                ColorPixels = OpenglGpuImpl.RenderbufferManager.CurrentDrawBuffer.RenderTarget.ReadPixels();

                                //Console.WriteLine($"{OpenglGpuImpl.RenderbufferManager.CurrentDrawBuffer.RenderTarget.ToString()} ColorPixels {ColorPixels.Length}");

                                if (ColorPixels.Length > 0)
                                    fixed (byte* pp = ColorPixels)
                                    {
                                        var rect = new SDL.SDL_Rect()
                                        { x = 0, y = 0, w = PspDisplay.MaxVisibleWidth, h = PspDisplay.MaxBufferHeight };
                                        SDL.SDL_UpdateTexture(texture, ref rect, new IntPtr(pp), PspDisplay.MaxBufferWidth * 4);
                                    }
                            }

                            //OpenglGpuImpl.RenderbufferManager.GetDrawBufferTextureAndLock(new DrawBufferKey()
                            //{
                            //    Address = display.CurrentInfo.FrameAddress,
                            //}, (DrawBuffer) =>
                            //{

                            //    if (DrawBuffer == null)
                            //    {
                            //        //Console.WriteLine("Not DrawBuffer");
                            //        return;
                            //    }
                            //    else
                            //    {
                            //        var RenderTarget = DrawBuffer.RenderTarget;
                            //        if (GL.glIsTexture(RenderTarget.TextureColor.Texture))
                            //        {
                            //            Color = RenderTarget.TextureColor;
                            //            Depth = RenderTarget.TextureDepth;

                            //            ColorPixels = Color.GetDataFromGpu();

                            //            if (ColorPixels.Length > 0)
                            //                fixed (byte* pp = ColorPixels)
                            //                {
                            //                    var rect = new SDL.SDL_Rect()
                            //                    { x = 0, y = 0, w = PspDisplay.MaxVisibleWidth, h = PspDisplay.MaxBufferHeight };
                            //                    SDL.SDL_UpdateTexture(texture, ref rect, new IntPtr(pp), PspDisplay.MaxBufferWidth * 4);
                            //                }

                            //            //Console.WriteLine($"ColorPixels {ColorPixels.Length}");
                            //            return;
                            //        }
                            //        else
                            //        {
                            //            Console.WriteLine("Not shared contexts");
                            //        }
                            //    }
                            //});

                            SDL.SDL_RenderCopy(renderer, texture, IntPtr.Zero, IntPtr.Zero);
                            SDL.SDL_RenderPresent(renderer);

                            display.TriggerVBlankEnd();
                        });
        }

    }
}