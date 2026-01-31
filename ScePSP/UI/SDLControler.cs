using ScePSP.Core;
using ScePSP.Types;
using System;
using System.Collections.Generic;
using System.IO;
using static SDL2.SDL;

namespace ScePSP.UI
{
    public class SDLControler
    {
        public nint controller;
        bool HasRumble;

        public enum PspCtrlAnalog
        {
            None = 0,
            Left = (1 << 0),
            Right = (1 << 1),
            Up = (1 << 2),
            Down = (1 << 3),
        }

        SceCtrlData ConCtrlData = new SceCtrlData
        {
            Buttons = PspCtrlButtons.None,
            Lx = 0,
            Ly = 0,
            TimeStamp = 0
        };

        public Dictionary<SDL_GameControllerButton, PspCtrlButtons> ControllerMap;

        public SDLControler()
        {
            SDL_Init(SDL_INIT_GAMECONTROLLER | SDL_INIT_HAPTIC);

            if (File.Exists(ApplicationPaths.AssertFolder + "/ControllerDB.txt"))
            {
                Console.WriteLine("ScePSP Load ControllerMappings...");
                SDL_GameControllerAddMappingsFromFile(ApplicationPaths.AssertFolder + "/ControllerDB.txt");
            }

            InitControllerMap();
        }

        ~SDLControler()
        {
            if (controller != 0)
            {
                SDL_GameControllerClose(controller);
                SDL_JoystickClose(controller);
            }
            SDL_Quit();
        }

        public void InitControllerMap()
        {
            ControllerMap = new()
            {
            { SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_A, PspCtrlButtons.Circle },
            { SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_B, PspCtrlButtons.Cross },
            { SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_X, PspCtrlButtons.Triangle },
            { SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_Y, PspCtrlButtons.Square },
            { SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_BACK, PspCtrlButtons.Select },
            { SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_START, PspCtrlButtons.Start },
            { SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_LEFTSHOULDER, PspCtrlButtons.LeftTrigger },
            { SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_RIGHTSHOULDER, PspCtrlButtons.RightTrigger },
            { SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_UP, PspCtrlButtons.Up },
            { SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_DOWN, PspCtrlButtons.Down },
            { SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_LEFT, PspCtrlButtons.Left },
            { SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_RIGHT, PspCtrlButtons.Right }
            };
        }

        public void CheckController()
        {
            if (controller == 0)
            {
                if (SDL_IsGameController(0) == SDL_bool.SDL_TRUE)
                {
                    controller = SDL_GameControllerOpen(0);
                }
                else
                {
                    controller = SDL_JoystickOpen(0);
                }
                if (controller != 0)
                {
                    HasRumble = SDL_GameControllerHasRumble(controller) == SDL_bool.SDL_TRUE;

                    Console.WriteLine($"Controller Device 1 : {SDL_JoystickNameForIndex(0)} Connected, Rumble: {HasRumble}");
                    if (HasRumble)
                        if (SDL_GameControllerRumble(controller, 0, 0, 0) != 0)
                        {
                            Console.WriteLine($"Controller 1 Rumble Error: {SDL_GetError()}");
                        }
                    SDL_Event dummyEvent;
                    SDL_PollEvent(out dummyEvent);
                }
            }
        }

        private void ButtonPress(PspCtrlButtons buttonMask, bool Down)
        {
            if (Down)
                ConCtrlData.Buttons |= buttonMask;
            else
                ConCtrlData.Buttons &= ~buttonMask;
        }

        public SceCtrlData QueryControllerState()
        {
            bool isPadPressed = false;
            foreach (SDL_GameControllerButton button in Enum.GetValues(typeof(SDL_GameControllerButton)))
            {
                bool isPressed = SDL_GameControllerGetButton(controller, button) == 1;
                if (isPressed)
                {
                    if (isPressed && (int)button >= 11 && (int)button <= 15)
                    {
                        isPadPressed = true;
                    }
                }
                if (ControllerMap.TryGetValue(button, out var gamepadInput))
                {
                    ButtonPress(gamepadInput, isPressed);
                }
            }

            //AnalogAxis
            float lx = 0.0f, ly = 0.0f, rx = 0.0f, ry = 0.0f;

            short leftX = SDL_GameControllerGetAxis(controller, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTX);
            short leftY = SDL_GameControllerGetAxis(controller, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTY);

            short rightX = SDL_GameControllerGetAxis(controller, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTX);
            short rightY = SDL_GameControllerGetAxis(controller, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTY);

            lx = NormalizeAxis(leftX);
            ly = NormalizeAxis(leftY);

            ConCtrlData.X = lx;
            ConCtrlData.Y = ly;

            rx = NormalizeAxis(rightX);
            ry = NormalizeAxis(rightY);

            //TRIGGER
            //short tl = SDL_GameControllerGetAxis(controller, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERLEFT);
            //short tr = SDL_GameControllerGetAxis(controller, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERRIGHT);

            if (isPadPressed) return ConCtrlData;

            //Hat
            int hatIndex = 0;
            int hatState = 0;
            IntPtr joystick = SDL_GameControllerGetJoystick(controller);
            if (joystick != IntPtr.Zero)
            {
                hatState = SDL_JoystickGetHat(joystick, hatIndex);

                ButtonPress(PspCtrlButtons.Up, (hatState & SDL_HAT_UP) != 0);
                ButtonPress(PspCtrlButtons.Down, (hatState & SDL_HAT_DOWN) != 0);
                ButtonPress(PspCtrlButtons.Left, (hatState & SDL_HAT_LEFT) != 0);
                ButtonPress(PspCtrlButtons.Right, (hatState & SDL_HAT_RIGHT) != 0);
            }

            if (hatState == 0)
            {
                ButtonPress(PspCtrlButtons.Up, ly < -0.5f);
                ButtonPress(PspCtrlButtons.Down, ly > 0.5f);
                ButtonPress(PspCtrlButtons.Left, lx < -0.5f);
                ButtonPress(PspCtrlButtons.Right, lx > 0.5f);
            }

            return ConCtrlData;
        }

        private float NormalizeAxis(short value)
        {
            float ret = Math.Clamp(value / 32767.0f, -1.0f, 1.0f);
            if (Math.Abs(ret) < 0.1f)
            {
                ret = 0.0f;
            }
            return ret;
        }
    }
}
