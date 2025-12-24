using ScePSP;
using ScePSP.Core;
using ScePSP.Core.Audio;
using ScePSP.Core.Audio.Impl.Null;
using ScePSP.Core.Components.Controller;
using ScePSP.Core.Components.Display;
using ScePSP.Core.Components.Rtc;
using ScePSP.Core.Gpu;
using ScePSP.Core.Memory;
using ScePSP.Core.Types.Controller;
using ScePSP.Hle.Modules.emulator;
using ScePSP.Runner;
using ScePSP.Runner.Components.Display;
using ScePSPUtils;
using ScePSPPlatform.GL;
using SDL2;
using System;
using System.Windows.Forms;

#pragma warning disable CS0436

class Program
{
    [STAThreadAttribute]
    static unsafe void Main(string[] args)
    {
        //GL.LoadAllOnce();

        if (SDL.SDL_Init(SDL.SDL_INIT_VIDEO | SDL.SDL_INIT_AUDIO) != 0)
        {
            Console.Error.WriteLine("Couldn't initialize SDL");
            return;
        }

        var window = SDL.SDL_CreateWindow(
            "ScePSP",
            SDL.SDL_WINDOWPOS_CENTERED, SDL.SDL_WINDOWPOS_CENTERED,
            PspDisplay.MaxVisibleWidth * 2, PspDisplay.MaxVisibleHeight * 2,
            SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN | SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE | SDL.SDL_WindowFlags.SDL_WINDOW_OPENGL
        );

        var renderer = SDL.SDL_CreateRenderer(window, -1, SDL.SDL_RendererFlags.SDL_RENDERER_PRESENTVSYNC | SDL.SDL_RendererFlags.SDL_RENDERER_ACCELERATED);

        var texture = SDL.SDL_CreateTexture(renderer, SDL.SDL_PIXELFORMAT_RGBA8888,
            (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_TARGET, PspDisplay.MaxVisibleWidth,
            PspDisplay.MaxVisibleHeight);

    LoadRom:
        OpenFileDialog ofn = new OpenFileDialog();
        ofn.Filter = "PSP Roms (*.pbp,*.iso, *.elf, *.zip)|*.pbp;*.iso;*.elf;*.zip";
        ofn.Title = "PSP Rom";
        if (ofn.ShowDialog() == DialogResult.Cancel) goto LoadRom;

        try
        {
            var injector = PspInjectContext.CreateInjectContext(PspStoredConfig.Load(), false);

            using var pspEmulator = injector.GetInstance<PspEmulator>();

            pspEmulator.StartAndLoad(ofn.FileName, GuiRunner: (emulator) =>
            {
                var running = true;

                var rtc = emulator.InjectContext.GetInstance<PspRtc>();
                var display = emulator.InjectContext.GetInstance<PspDisplay>();
                var displayComponent = emulator.InjectContext.GetInstance<DisplayComponentThread>();
                var memory = emulator.InjectContext.GetInstance<PspMemory>();
                var controller = pspEmulator.InjectContext.GetInstance<PspController>();

                displayComponent.triggerStuff = false;

                //emulator.InjectContext.SetInstanceType<GpuImpl, GpuImplSoft>();
                //emulator.InjectContext.SetInstanceType<PspAudioImpl, AudioImplNull>();

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
                                };

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


                    {
                        //Console.WriteLine(display.CurrentInfo.FrameAddress);
                        var pixels2 = new uint[PspDisplay.MaxBufferArea];
                        var displayData = memory.Range<uint>(display.CurrentInfo.FrameAddress, PspDisplay.MaxBufferArea);
                        for (var m = 0; m < PspDisplay.MaxBufferArea; m++)
                        {
                            var color = displayData[m];
                            var r = color.Extract(0, 8);
                            var g = color.Extract(8, 8);
                            var b = color.Extract(16, 8);
                            pixels2[m] = (r << 24) | (g << 16) | (b << 8) | 0xFF;
                        }

                        fixed (uint* pp = pixels2)
                        {
                            var rect = new SDL.SDL_Rect()
                            { x = 0, y = 0, w = PspDisplay.MaxVisibleWidth, h = PspDisplay.MaxBufferHeight };
                            SDL.SDL_UpdateTexture(texture, ref rect, new IntPtr(pp), PspDisplay.MaxBufferWidth * 4);
                        }
                    }

                    displayComponent.Step(DrawStart: () => { display.TriggerDrawStart(); },
                        VBlankStart: () => { display.TriggerVBlankStart(); }, VBlankEnd: () =>
                        {
                            lx = pressingAnalogLeft != 0 ? -pressingAnalogLeft : pressingAnalogRight;
                            ly = pressingAnalogUp != 0 ? -pressingAnalogUp : pressingAnalogDown;

                            ctrlData.X = lx / 3f;
                            ctrlData.Y = ly / 3f;
                            ctrlData.TimeStamp = (uint)rtc.UnixTimeStampTS.Milliseconds;

                            controller.InsertSceCtrlData(ctrlData);

                            SDL.SDL_RenderCopy(renderer, texture, IntPtr.Zero, IntPtr.Zero);
                            SDL.SDL_RenderPresent(renderer);

                            display.TriggerVBlankEnd();
                        });
                    //display.TriggerVBlankStart();

                    //display.TriggerVBlankEnd();
                }

            }, false, false, false);
        }
        finally
        {
            SDL.SDL_DestroyTexture(texture);
            SDL.SDL_DestroyRenderer(renderer);
            SDL.SDL_DestroyWindow(window);
            SDL.SDL_Quit();
        }
    }
}