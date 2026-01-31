using ScePSP.cheats;
using ScePSP.Components.Display;
using ScePSP.Core;
using ScePSPUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace ScePSP.UI
{
    public partial class CheatForm : Form
    {
        public class CheatItem
        {
            public string Name;
            public bool Enabled;
            public string[] Code;
            public Queue<uint> CodeValues = new Queue<uint>();
        }

        private List<CheatItem> CheatItems = new List<CheatItem>();
        private string LinkedCwcheatsFile;
        private string[] CurrentCheats;

        DisplayConfig DisplayConfig;
        CWCheatList CWCheatList;

        public CheatForm()
        {
            this.InitializeComponent();
        }

        private void LoadCheat()
        {
            LinkedCwcheatsFile = ApplicationPaths.AssertFolder + "/" + DisplayConfig.ID + ".cwc";

            if (File.Exists(LinkedCwcheatsFile))
            {
                CheatItems.Clear();

                ConsoleUtils.SaveRestoreConsoleColor(ConsoleColor.Magenta, () => { Console.WriteLine("Loaded Cheat... {0}", DisplayConfig.ID); });
                CurrentCheats = File.ReadAllLines(LinkedCwcheatsFile);
                EdCheatCode.Text = File.ReadAllText(LinkedCwcheatsFile);
                ParseCwCheat(CurrentCheats);
            }

            LvCheats.Clear();
            foreach (var cheat in CheatItems)
            {
                var lvitem = LvCheats.Items.Add(cheat.Name);
                lvitem.Checked = cheat.Enabled;
                lvitem.Tag = cheat;
            }
        }

        private void ParseCwCheat(string[] lines)
        {
            CheatItem currentItem = null;
            List<string> currentCodes = new List<string>();

            foreach (var rawLine in lines)
            {
                string line = rawLine.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith("//") || line.StartsWith(";") || line.StartsWith("#"))
                    continue;

                if (line.StartsWith("_C"))
                {
                    if (currentItem != null)
                    {
                        currentItem.Code = currentCodes.ToArray();
                        CheatItems.Add(currentItem);
                        currentCodes.Clear();
                    }

                    string[] parts = line.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                    currentItem = new CheatItem();
                    currentItem.Enabled = parts[0].Contains("E");
                    currentItem.Name = parts.Length > 1 ? parts[1].Trim() : $"NoName {parts[0]}";
                }
                else if (line.StartsWith("_L") && currentItem != null)
                {
                    currentCodes.Add(line);
                    var l = line.Trim();
                    var Parts = l.Split(' ', '\t');
                    foreach (var Part in Parts)
                    {
                        if (Part.Substr(0, 2) == "0x")
                        {
                            currentItem.CodeValues.Enqueue((uint)NumberUtils.ParseIntegerConstant(Part));
                        }
                    }
                }
            }

            if (currentItem != null)
            {
                currentItem.Code = currentCodes.ToArray();
                CheatItems.Add(currentItem);
            }
        }

        private void ApplyCheat()
        {
            CWCheatList.Clear();
            foreach (ListViewItem cheat in LvCheats.Items)
            {
                if (cheat.Checked)
                {
                    var item = cheat.Tag as CheatItem;
                    CWCheatList.Add(item.CodeValues);
                }
            }
        }

        private void CheatsForm_Shown(object sender, EventArgs e)
        {
            DisplayConfig = MainForm.Context.GetInstance<DisplayConfig>();
            CWCheatList = MainForm.Context.GetInstance<CWCheatList>();

            LoadCheat();
        }

        private void BtnParse_Click(object sender, EventArgs e)
        {
            CurrentCheats = EdCheatCode.Text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            CheatItems.Clear();
            ParseCwCheat(CurrentCheats);
            LvCheats.Clear();
            foreach (var cheat in CheatItems)
            {
                var lvitem = LvCheats.Items.Add(cheat.Name);
                lvitem.Checked = cheat.Enabled;
                lvitem.Tag = cheat;
            }
        }

        private void ApplyButton_Click(object sender, EventArgs e)
        {
            CurrentCheats = EdCheatCode.Text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            File.WriteAllLines(LinkedCwcheatsFile, CurrentCheats);

            ApplyCheat();
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            base.Close();
        }

        private void CheatsTextBox_TextChanged(object sender, EventArgs e)
        {
        }

        private void CheatsForm_Load(object sender, EventArgs e)
        {
        }

        private void tableLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {

        }
    }
}
