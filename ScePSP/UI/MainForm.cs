using ScePSP.BackEnd;
using ScePSP.BackEnd.OpenGL;
using ScePSP.Core;
using ScePSP.Devices.Display;
using ScePSP.GE;
using ScePSP.Hle;
using ScePSP.Memory;
using ScePSP.Runner;
using ScePSP.Types;
using ScePSP.Types.Controller;
using ScePSPUtils;
using ScePSPUtils.Extensions;
using ScePSX.UI;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static ScePSPUtils.Logger;

namespace ScePSP.UI
{
    public partial class MainForm : Form
    {
        public IntPtr window;

        public SceCtrlData ctrlData;
        public int lx, ly;
        public int pressingAnalogLeft, pressingAnalogRight, pressingAnalogUp, pressingAnalogDown;

        public static PspEmulator pspEmulator;
        PspStoredConfig cfg;
        ControllerConfig keymap;

        NullRenderer nullrender;
        RomList RomList;

        string Title_O = "ScePSP Alpha[260120]", Title;
        private readonly System.Diagnostics.Stopwatch _stopwatch = System.Diagnostics.Stopwatch.StartNew();
        private int _frameCount;
        public float CurrentFPS { get; private set; }

        Dictionary<Keys, PspCtrlButtons> KeyMap;
        Dictionary<Keys, PspCtrlAnalog> AnalogKeyMap;
        [Flags]
        public enum PspCtrlAnalog
        {
            None = 0,
            Left = (1 << 0),
            Right = (1 << 1),
            Up = (1 << 2),
            Down = (1 << 3),
        }
        bool AnalogUp = false;
        bool AnalogDown = false;
        bool AnalogLeft = false;
        bool AnalogRight = false;
        float AnalogX = 0.0f, AnalogY = 0.0f;
        internal SceCtrlData SceCtrlData;

        public MainForm()
        {
            InitializeComponent();

            Text = Title_O;

            ctrlData = new SceCtrlData { Buttons = 0, Lx = 0, Ly = 0 };
            lx = 0;
            ly = 0;
            pressingAnalogLeft = 0;
            pressingAnalogRight = 0;
            pressingAnalogUp = 0;
            pressingAnalogDown = 0;

            Logger.OnGlobalLog += Log;
            PspDisplay.DrawEvent += DrawEvent;

            RomList = new RomList();
            RomList.Dock = DockStyle.Fill;
            RomList.Enabled = true;
            RomList.Visible = true;
            panel1.Controls.Add(RomList);
            RomList.BringToFront();

            nullrender = new NullRenderer();
            nullrender.Initialize(panel1);

            cfg = PspStoredConfig.Load();
            keymap = cfg.ControllerConfig;
            SetKeyMap();
            MnuH264.Checked = cfg.H264Enable;
            DbgHleCall.Checked = cfg.TrackHLECalls;

            RomList.DoubleClick += RomList_DoubleClick;
        }

        private void RomList_DoubleClick(object sender, EventArgs e)
        {
            if (RomList.SelectedIndex == -1) return;
            var Item = RomList.Entries[RomList.SelectedIndex];
            if (File.Exists(Item.IsoFile))
            {
                RunPSP(Item.IsoFile);
            }
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            RomList.Items.Clear();
            if (cfg.IsosPath != "" && cfg.IsosPath != null)
                RomList.ScanPath(cfg.IsosPath, "./assert/", 255);

            RomList.UpdateScrollBar();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            if (cfg.FromPos == null) return;
            string[] PosStr = cfg.FromPos.Split('|');
            if (PosStr.Length >= 4)
            {
                this.Location = new Point(Convert.ToInt16(PosStr[0]), Convert.ToInt16(PosStr[1]));
                this.Size = new Size(Convert.ToInt16(PosStr[2]), Convert.ToInt16(PosStr[3]));
                Rectangle screenBounds = Screen.PrimaryScreen.WorkingArea;
                if (!screenBounds.Contains(this.Bounds))
                {
                    this.Location = new Point(0, 0);
                }
            }
        }

        private void MnuSetPath_Click(object sender, EventArgs e)
        {
            var Dialog = new FolderBrowserDialog();
            Dialog.ShowNewFolderButton = true;
            //Dialog.RootFolder = PspConfig.StoredConfig.IsosPath;
            if (Dialog.ShowDialog() == DialogResult.OK)
            {
                cfg.IsosPath = Dialog.SelectedPath;
                cfg.Save();
            }
            MainForm_Shown(null, null);
        }

        private void MnuLoad_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofn = new OpenFileDialog();
            ofn.Filter = "PSP Roms (*.pbp, *.prx, *.iso, &.cso, *.dax, *.elf, *.zip)|*.pbp;*.prx;*.iso;*.cso;*.dax;*.elf;*.zip";
            ofn.Title = "PSP Rom";
            if (ofn.ShowDialog() == DialogResult.Cancel) return;

            RunPSP(ofn.FileName);
        }

        private void RunPSP(string file)
        {
            RomList.Enabled = false;
            if (pspEmulator != null) FreePSP();
            nullrender.BringToFront();
            this.Focus();

            cfg.H264Enable = MnuH264.Checked;
            cfg.TrackHLECalls = DbgHleCall.Checked;

            pspEmulator = new PspEmulator();

            PSPDrivers.Config.StoredConfig = cfg;

            pspEmulator.Start(file, cfg.TrackHLECalls, false, NullRenderer.hwnd);

            PSPDrivers.Config.DisplayConfig.H264Enabled = cfg.H264Enable;

            Title = Title_O;
            if (PSPDrivers.GameInfo.ID != null)
            {
                Title += " - " + PSPDrivers.GameInfo.ID;
            }
            if (PSPDrivers.GameInfo.Title != null)
            {
                Title += " - " + PSPDrivers.GameInfo.Title;
            }
            Title += " - " + cfg.RenderScale + "xIR";
            if (cfg.TexScaleType >= 1)
                Title += " - " + cfg.TexScale + "*" + (ScaleMode)cfg.TexScaleType;
        }

        private void FreePSP()
        {
            pspEmulator.Stop();
            pspEmulator = null;
            GC.Collect();
        }

        private void Log(string name, Level level, string message, StackFrame stack)
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

        private void DrawEvent()
        {
            SceCtrlData.X = 0;
            SceCtrlData.Y = 0;

            bool AnalogXUpdated = false;
            bool AnalogYUpdated = false;
            if (AnalogUp) { AnalogY -= 0.4f; AnalogYUpdated = true; }
            if (AnalogDown) { AnalogY += 0.4f; AnalogYUpdated = true; }
            if (AnalogLeft) { AnalogX -= 0.4f; AnalogXUpdated = true; }
            if (AnalogRight) { AnalogX += 0.4f; AnalogXUpdated = true; }
            if (!AnalogXUpdated) AnalogX /= 2.0f;
            if (!AnalogYUpdated) AnalogY /= 2.0f;

            AnalogX = MathFloat.Clamp(AnalogX, -1.0f, 1.0f);
            AnalogY = MathFloat.Clamp(AnalogY, -1.0f, 1.0f);

            //Console.WriteLine("{0}, {1}", AnalogX, AnalogY);

            SceCtrlData.X = AnalogX;
            SceCtrlData.Y = AnalogY;

            ctrlData.TimeStamp = (uint)PSPDrivers.PspRtc.UnixTimeStampTS.Milliseconds;

            PSPDrivers.Devices.Controller.InsertSceCtrlData(ctrlData);

            PSPDrivers.Config.DisplayConfig.Width = panel1.Width;
            PSPDrivers.Config.DisplayConfig.Height = panel1.Height;

            UpdateFrame();

            this.Text = this.Title + $" - [ {CurrentFPS:F2} FPS ]";
        }

        private void MnuFunc_Click(object sender, EventArgs e)
        {
            if (pspEmulator == null || !pspEmulator.Runing) return;

            pspEmulator.Pause();

            FunctionForm funForm = new FunctionForm();

            ShowFrom(funForm);
        }

        private void MnuTexture_Click(object sender, EventArgs e)
        {
            if (pspEmulator == null || !pspEmulator.Runing) return;

            pspEmulator.Pause();

            TextureForm textureForm = new TextureForm();

            ShowFrom(textureForm);
        }

        private void MnuKeyConfig_Click(object sender, EventArgs e)
        {
            ButtonForm buttonForm = new ButtonForm(cfg);

            ShowFrom(buttonForm);
        }

        private void DbgHleCall_Click(object sender, EventArgs e)
        {
            if (pspEmulator == null || !pspEmulator.Runing) return;
        }

        private void MnuExit_Click(object sender, EventArgs e)
        {
            if (pspEmulator != null) FreePSP();
            RomList.BringToFront();
            RomList.Enabled = true;
            Text = Title_O;
        }

        private void ShowFrom(Form Frm)
        {
            Frm.StartPosition = FormStartPosition.Manual;
            Frm.Owner = this;
            Frm.FormClosed += CloseFrom;

            Point parentCenterClient = new Point(
                this.ClientSize.Width / 2,
                this.ClientSize.Height / 2
            );

            Point parentCenterScreen = this.PointToScreen(parentCenterClient);

            Frm.Location = new Point(
                parentCenterScreen.X - Frm.Width / 2,
                parentCenterScreen.Y - Frm.Height / 2
                );

            Frm.Show();
        }

        private void CloseFrom(object sender, FormClosedEventArgs e)
        {
            SetKeyMap();

            if (pspEmulator == null || !pspEmulator.Runing) return;

            pspEmulator.Resume();
        }

        public Keys StringToKey(string keyStr)
        {
            if (string.IsNullOrWhiteSpace(keyStr)) return Keys.None;

            if (Enum.TryParse<Keys>(keyStr, true, out Keys result))
            {
                return result;
            }
            return Keys.None;
        }

        private void TryUpdateAnalog(Keys Key, bool Press)
        {
            switch (AnalogKeyMap.GetOrDefault(Key, PspCtrlAnalog.None))
            {
                case PspCtrlAnalog.Up: AnalogUp = Press; break;
                case PspCtrlAnalog.Down: AnalogDown = Press; break;
                case PspCtrlAnalog.Left: AnalogLeft = Press; break;
                case PspCtrlAnalog.Right: AnalogRight = Press; break;
            }
        }

        private void SetKeyMap()
        {
            KeyMap = new Dictionary<Keys, PspCtrlButtons>();
            {
                KeyMap[StringToKey(keymap.DigitalLeft)] = PspCtrlButtons.Left;
                KeyMap[StringToKey(keymap.DigitalRight)] = PspCtrlButtons.Right;
                KeyMap[StringToKey(keymap.DigitalUp)] = PspCtrlButtons.Up;
                KeyMap[StringToKey(keymap.DigitalDown)] = PspCtrlButtons.Down;

                KeyMap[StringToKey(keymap.TriangleButton)] = PspCtrlButtons.Triangle;
                KeyMap[StringToKey(keymap.CrossButton)] = PspCtrlButtons.Cross;
                KeyMap[StringToKey(keymap.SquareButton)] = PspCtrlButtons.Square;
                KeyMap[StringToKey(keymap.CircleButton)] = PspCtrlButtons.Circle;

                KeyMap[StringToKey(keymap.StartButton)] = PspCtrlButtons.Start;
                KeyMap[StringToKey(keymap.SelectButton)] = PspCtrlButtons.Select;

                KeyMap[StringToKey(keymap.LeftTriggerButton)] = PspCtrlButtons.LeftTrigger;
                KeyMap[StringToKey(keymap.RightTriggerButton)] = PspCtrlButtons.RightTrigger;
            }
            AnalogKeyMap = new Dictionary<Keys, PspCtrlAnalog>();
            {
                AnalogKeyMap[StringToKey(keymap.AnalogLeft)] = PspCtrlAnalog.Left;
                AnalogKeyMap[StringToKey(keymap.AnalogRight)] = PspCtrlAnalog.Right;
                AnalogKeyMap[StringToKey(keymap.AnalogUp)] = PspCtrlAnalog.Up;
                AnalogKeyMap[StringToKey(keymap.AnalogDown)] = PspCtrlAnalog.Down;
            }
        }

        private void SKeyPress(KeyEventArgs e, bool Down)
        {
            if (pspEmulator == null || !pspEmulator.Runing) return;

            PspCtrlButtons buttonMask = KeyMap.GetOrDefault(e.KeyCode, PspCtrlButtons.None);
            TryUpdateAnalog(e.KeyCode, Down);
            if (Down)
                ctrlData.Buttons |= buttonMask;
            else
                ctrlData.Buttons &= ~buttonMask;
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Tab:
                    PSPDrivers.Tasks.DisplayTask.FullSpeed = true;
                    break;
            }
            SKeyPress(e, true);
        }

        private void MainForm_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Tab:
                    PSPDrivers.Tasks.DisplayTask.FullSpeed = false;
                    break;
            }
            SKeyPress(e, false);
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (pspEmulator != null) FreePSP();
            cfg.FromPos = $"{this.Location.X}|{this.Location.Y}|{this.Size.Width}|{this.Size.Height}";
            cfg.H264Enable = MnuH264.Checked;
            cfg.TrackHLECalls = DbgHleCall.Checked;
            cfg.Save();
        }

        private void MnuOpenCfg_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("notepad.exe", "./memstick/Config.xml");

        }

        private void mnugithublink_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("cmd", "/c start https://github.com/unknowall/ScePSP");
        }
    }
}
