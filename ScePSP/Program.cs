using ScePSP;
using ScePSP.Core.Components.Display;
using ScePSP.Core.Types.Controller;
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

    static SceCtrlData ctrlData;
    static int lx, ly;
    static int pressingAnalogLeft, pressingAnalogRight, pressingAnalogUp, pressingAnalogDown;

    static PspEmulator pspEmulator;

    static string title;

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
            SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN | SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE// | SDL.SDL_WindowFlags.SDL_WINDOW_OPENGL
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

        Logger.OnGlobalLog += Log;

        PspDisplay.DrawEvent += DrawEvent;

        ctrlData = new SceCtrlData { Buttons = 0, Lx = 0, Ly = 0 };
        lx = 0;
        ly = 0;
        pressingAnalogLeft = 0;
        pressingAnalogRight = 0;
        pressingAnalogUp = 0;
        pressingAnalogDown = 0;

        pspEmulator = new PspEmulator();

        pspEmulator.Start(ofn.FileName, false, false, windowhwnd);

        title = "ScePSP";
        if (PSPDrivers.GameInfo.ID != null)
        {
            title += " - " + PSPDrivers.GameInfo.ID;
        }
        if (PSPDrivers.GameInfo.Title != "")
        {
            title += " - " + PSPDrivers.GameInfo.Title;
        }
        SDL.SDL_SetWindowTitle(window, title);

        RunMainLoop();
    }

    private static void Log(string name, Level level, string message, StackFrame stack)
    {
        switch (level)
        {
            //case Level.Notice:
            case Level.Fatal:
            case Level.Warning:
            case Level.Error:
                Console.WriteLine($"[{level}] {name}: {message}");
                break;
        }
    }

    private static void DrawEvent()
    {
        lx = pressingAnalogLeft != 0 ? -pressingAnalogLeft : pressingAnalogRight;
        ly = pressingAnalogUp != 0 ? -pressingAnalogUp : pressingAnalogDown;

        ctrlData.X = lx / 3f;
        ctrlData.Y = ly / 3f;

        ctrlData.TimeStamp = (uint)PSPDrivers.PspRtc.UnixTimeStampTS.Milliseconds;

        PSPDrivers.Devices.PspController.InsertSceCtrlData(ctrlData);

        SDL.SDL_GetWindowSize(window,
           out PSPDrivers.Config.DisplayConfig.Width,
           out PSPDrivers.Config.DisplayConfig.Height);

        SDL.SDL_SetWindowTitle(window, title+$" - [ {PSPDrivers.Tasks.DisplayTask.CurrentFPS:F2} FPS ]");
    }

    private static void RunMainLoop()
    {
        var running = true;

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
            //Thread.Sleep(10);

            while (SDL.SDL_PollEvent(out var e) != 0)
            {
                switch (e.type)
                {
                    case SDL.SDL_EventType.SDL_QUIT:
                        running = false;
                        pspEmulator.Stop();
                        break;
                    case SDL.SDL_EventType.SDL_KEYDOWN:
                    case SDL.SDL_EventType.SDL_KEYUP:
                        var pressed = e.type == SDL.SDL_EventType.SDL_KEYDOWN;
                        PspCtrlButtons buttonMask;
                        switch (e.key.keysym.sym)
                        {
                            case SDL.SDL_Keycode.SDLK_u:
                                buttonMask = PspCtrlButtons.Square;
                                break;
                            case SDL.SDL_Keycode.SDLK_i:
                                buttonMask = PspCtrlButtons.Triangle;
                                break;
                            case SDL.SDL_Keycode.SDLK_j:
                                buttonMask = PspCtrlButtons.Circle;
                                break;
                            case SDL.SDL_Keycode.SDLK_k:
                                buttonMask = PspCtrlButtons.Cross;
                                break;
                            case SDL.SDL_Keycode.SDLK_SPACE:
                                buttonMask = PspCtrlButtons.Select;
                                break;
                            case SDL.SDL_Keycode.SDLK_RETURN:
                                buttonMask = PspCtrlButtons.Start;
                                break;
                            case SDL.SDL_Keycode.SDLK_w:
                                buttonMask = PspCtrlButtons.Up;
                                break;
                            case SDL.SDL_Keycode.SDLK_s:
                                buttonMask = PspCtrlButtons.Down;
                                break;
                            case SDL.SDL_Keycode.SDLK_a:
                                buttonMask = PspCtrlButtons.Left;
                                break;
                            case SDL.SDL_Keycode.SDLK_d:
                                buttonMask = PspCtrlButtons.Right;
                                break;
                            case SDL.SDL_Keycode.SDLK_UP:
                                buttonMask = UpdatePressing(ref pressingAnalogUp, pressed);
                                break;
                            case SDL.SDL_Keycode.SDLK_DOWN:
                                buttonMask = UpdatePressing(ref pressingAnalogDown, pressed);
                                break;
                            case SDL.SDL_Keycode.SDLK_LEFT:
                                buttonMask = UpdatePressing(ref pressingAnalogLeft, pressed);
                                break;
                            case SDL.SDL_Keycode.SDLK_RIGHT:
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