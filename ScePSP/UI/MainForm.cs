using ScePSP.Audio;
using ScePSP.Components.Display;
using ScePSP.Controller;
using ScePSP.Core;
using ScePSP.Cpu;
using ScePSP.Display;
using ScePSP.GE;
using ScePSP.Rtc;
using ScePSP.Runner;
using ScePSP.Runner.Display;
using ScePSP.Types;
using ScePSPUtils;
using ScePSPUtils.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using static ScePSPUtils.Logger;

namespace ScePSP.UI
{
    public partial class MainForm : Form
    {
        public Runner.Runner Runner;
        PspStoredConfig cfg;
        public static PspContext Context;
        public PspRtc Rtc;
        public PspController Controller;
        public DisplayConfig DisplayConfig;
        public DisplayThread Display;
        ControllerConfig keymap;

        NullRenderer nullrender;
        RomList RomList;

        public static string Title_O = "ScePSP Alpha 0.0.2";
        string Title;

        Dictionary<Keys, PspCtrlButtons> KeyMap;
        Dictionary<Keys, PspCtrlAnalog> AnalogKeyMap;
        SceCtrlData ctrlData = new SceCtrlData
        {
            Buttons = PspCtrlButtons.None,
            Lx = 0,
            Ly = 0
        };
        SceCtrlData ConCtrlData = new SceCtrlData();

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

        SDLControler SdlController = new SDLControler();

        [DllImport("kernel32.dll")]
        public static extern Boolean AllocConsole();
        [DllImport("kernel32.dll")]
        public static extern Boolean FreeConsole();

        public MainForm()
        {
            InitializeComponent();

            AllocConsole();

            CheckForIllegalCrossThreadCalls = false;

            Text = Title_O;

            ctrlData = new SceCtrlData { Buttons = 0, Lx = 0, Ly = 0 };

            RomList = new RomList();
            RomList.Dock = DockStyle.Fill;
            RomList.Enabled = true;
            RomList.Visible = true;
            panel1.Controls.Add(RomList);
            RomList.BringToFront();

            PspDisplay.DrawEvent += DrawEvent;
            Logger.OnGlobalLog += Log;

            nullrender = new NullRenderer();
            nullrender.Initialize(panel1);

            cfg = PspStoredConfig.Load();
            keymap = cfg.ControllerConfig;
            SetKeyMap();
            MnuH264.Checked = cfg.H264Enable;
            DbgHleCall.Checked = cfg.TrackHLECalls;

            RomList.DoubleClick += RomList_DoubleClick;

            Translations.DefaultLanguage = "en";
            Translations.Init();
            Translations.UpdateLang(this);
        }

        private void RomList_DoubleClick(object sender, EventArgs e)
        {
            if (RomList.SelectedIndex == -1) return;
            var Item = RomList.Entries[RomList.SelectedIndex];
            if (File.Exists(Item.IsoFile))
            {
                RunGame(Item.IsoFile, nullrender.Handle);
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

            RunGame(ofn.FileName, nullrender.Handle);
        }

        string isofile;

        private void RunGame(string FileName, IntPtr Window)
        {
            RomList.Enabled = false;
            nullrender.BringToFront();
            this.Focus();

            isofile = FileName;

            DynarecConfig.AllowCreatingUsedFunctionsInBackground = cfg.ScanCreatingFunctions;
            DynarecConfig.EnableTailCalling = cfg.TailCall;
            DynarecConfig.EmitCallTick = cfg.TickCall;
            DynarecConfig.EnableFastPspMemoryUtilsGetFastMemoryReader = cfg.FastMemoryReader;

            Context = PSP.Create(cfg, Window);
            Runner = Context.GetInstance<Runner.Runner>();
            Rtc = Context.GetInstance<PspRtc>();
            Controller = Context.GetInstance<PspController>();
            DisplayConfig = Context.GetInstance<DisplayConfig>();
            Display = Context.GetInstance<DisplayThread>();

            Runner.StartSynchronized();

            Runner.CpuThread.ThreadTaskQueue.EnqueueAndWaitCompleted(() =>
            {
                Runner.CpuThread._LoadFile(FileName);
            });

            Title = Title_O;
            if (DisplayConfig.ID != null)
            {
                Title += " - " + DisplayConfig.ID;
            }
            if (DisplayConfig.Title != "")
            {
                Title += " - " + DisplayConfig.Title;
            }
        }

        private void FreePSP()
        {
            PspDisplay.DrawEvent -= DrawEvent;
            Logger.OnGlobalLog -= Log;

            Runner.StopSynchronized();
            Context.GetInstance<GEBackEnd>().StopSynchronized();
            Context.GetInstance<AudioBackEnd>().StopSynchronized();
            Context.Dispose();
            Context = null;
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

        private void DrawEvent()
        {
            ctrlData.X = 0;
            ctrlData.Y = 0;

            bool AnalogXUpdated = false;
            bool AnalogYUpdated = false;
            if (AnalogUp) { AnalogY -= 0.4f; AnalogYUpdated = true; }
            if (AnalogDown) { AnalogY += 0.4f; AnalogYUpdated = true; }
            if (AnalogLeft) { AnalogX -= 0.4f; AnalogXUpdated = true; }
            if (AnalogRight) { AnalogX += 0.4f; AnalogXUpdated = true; }
            if (!AnalogXUpdated) AnalogX /= 3.0f;
            if (!AnalogYUpdated) AnalogY /= 3.0f;

            AnalogX = MathFloat.Clamp(AnalogX, -1.0f, 1.0f);
            AnalogY = MathFloat.Clamp(AnalogY, -1.0f, 1.0f);

            //Console.WriteLine("{0}, {1}", AnalogX, AnalogY);

            ctrlData.X = AnalogX;
            ctrlData.Y = AnalogY;

            ctrlData.TimeStamp = (uint)Rtc.Elapsed.TotalMilliseconds;
            ConCtrlData.TimeStamp = ctrlData.TimeStamp;

            if (SdlController.controller == 0)
                SdlController.CheckController();
            else
                ConCtrlData = SdlController.QueryControllerState();

            if (AnalogX != 0 || AnalogY != 0 || ctrlData.Buttons != PspCtrlButtons.None)
                Controller.InsertSceCtrlData(ctrlData);
            else
                Controller.InsertSceCtrlData(ConCtrlData);

            DisplayConfig.Width = panel1.Width;
            DisplayConfig.Height = panel1.Height;

            this.Text = this.Title;// + $" - [ {Display.CurrentFPS:F2} FPS ]";
        }

        private void MnuFunc_Click(object sender, EventArgs e)
        {
            if (Context == null || !Runner.Runing) return;

            Runner.PauseSynchronized();

            MethodForm funForm = new MethodForm();

            ShowFrom(funForm);
        }

        private void MnuSubFunc_Click(object sender, EventArgs e)
        {
            if (Context == null || !Runner.Runing) return;

            Runner.PauseSynchronized();

            FuncForm funForm = new FuncForm();

            ShowFrom(funForm);
        }

        private void MnuCheat_Click(object sender, EventArgs e)
        {
            if (Context == null || !Runner.Runing) return;

            Runner.PauseSynchronized();

            CheatForm frm = new CheatForm();

            ShowFrom(frm);
        }

        private void MnuTexture_Click(object sender, EventArgs e)
        {
            if (Context == null || !Runner.Runing) return;

            Runner.PauseSynchronized();

            TextureForm Form = new TextureForm();

            ShowFrom(Form);
        }

        private void MnuFileView_Click(object sender, EventArgs e)
        {
            if (Context == null || !Runner.Runing) return;

            FileExtract Form = new FileExtract(isofile);

            ShowFrom(Form);
        }

        private void MnuKeyConfig_Click(object sender, EventArgs e)
        {
            ButtonForm Form = new ButtonForm(cfg);

            ShowFrom(Form);
        }

        private void MnuSet_Click(object sender, EventArgs e)
        {
            SetForm Form = new SetForm(cfg);

            ShowFrom(Form);
        }

        private void MnuMem_Click(object sender, EventArgs e)
        {
            if (Context == null || !Runner.Runing) return;

            MemForm Form = new MemForm(Context);

            ShowFrom(Form);
        }

        private void MnuAbout_Click(object sender, EventArgs e)
        {
            FrmAbout Form = new FrmAbout();

            ShowFrom(Form, false);
        }

        private void DbgHleCall_Click(object sender, EventArgs e)
        {
            if (Context == null || !Runner.Runing) return;
        }

        private void MnuExit_Click(object sender, EventArgs e)
        {
            if (Context != null) FreePSP();
            RomList.BringToFront();
            RomList.Enabled = true;
            Text = Title_O;
        }

        private void ShowFrom(Form Frm, bool UpdateLang = true)
        {
            if (UpdateLang) Translations.UpdateLang(Frm);
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

            if (Context == null || !Runner.Runing) return;

            Runner.ResumeSynchronized();
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
            keymap = cfg.ControllerConfig;

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
            if (Context == null || !Runner.Runing) return;

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
                    Display.FullSpeed = true;
                    break;
            }
            SKeyPress(e, true);
        }

        private void MainForm_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Tab:
                    Display.FullSpeed = false;
                    break;
            }
            SKeyPress(e, false);
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (Context != null) FreePSP();
            cfg.FromPos = $"{this.Location.X}|{this.Location.Y}|{this.Size.Width}|{this.Size.Height}";
            cfg.H264Enable = MnuH264.Checked;
            cfg.TrackHLECalls = DbgHleCall.Checked;
            cfg.Save();
        }

        private void MnuOpenCfg_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("notepad.exe", "./assert/Config.xml");

        }

        private void mnugithublink_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("cmd", "/c start https://github.com/unknowall/ScePSP");
        }
    }
}
