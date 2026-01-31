using SafeILGenerator.Ast.Nodes;
using SafeILGenerator.Ast.Serializers;
using ScePSP.Cpu;
using ScePSP.Cpu.Assembler;
using ScePSP.Cpu.Dynarec;
using ScePSPUtils.Drawing;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ScePSP.UI
{
    public partial class FuncForm : Form
    {
        public CpuProcessor CpuProcessor;

        public FuncForm()
        {
            InitializeComponent();
        }

        private void FunctionForm_Shown(object sender, EventArgs e)
        {
            CpuProcessor = MainForm.Context.GetInstance<CpuProcessor>();

            FunctionViewerForm_Load();
        }

        private void BtnGO_Click(object sender, EventArgs e)
        {
            foreach (PCItem item in PcListBox.Items)
            {
                if (item.PC == Convert.ToInt32(EdFind.Text, 16))
                {
                    PcListBox.SelectedItem = item;
                    UpdateText();
                    break;
                }
            }
        }

        public class PCItem
        {
            public DynarecFunction FunctionCache;
            public uint PC;

            public Color ItemColor
            {
                get
                {
                    if (FunctionCache.Name != "") return Color.Blue;
                    if (FunctionCache.TimeTotal >= TimeSpan.FromMilliseconds(40)) return ColorUtils.Mix(Color.White, Color.Red, 1);
                    if (FunctionCache.TimeTotal >= TimeSpan.FromMilliseconds(20)) return ColorUtils.Mix(Color.White, Color.Red, 0.75);
                    if (FunctionCache.TimeTotal >= TimeSpan.FromMilliseconds(10)) return ColorUtils.Mix(Color.White, Color.Red, 0.5);
                    return Color.Black;
                }
            }

            public string Message
            {
                get
                {
                    return FunctionCache.Name != "" ? FunctionCache.Name : $"0x{PC:X}";
                }
            }

            public override string ToString()
            {
                return Message;
            }
        }

        private void FunctionViewerForm_Load()
        {
            PcListBox.SuspendLayout();
            foreach (var item in CpuProcessor.MethodCache.Functions.Values)
            {
                if (item.AstNode != null)
                {
                    PcListBox.Items.Add(new PCItem()
                    {
                        FunctionCache = item,
                        PC = item.EntryPC,
                    });
                }
            }
            LanguageComboBox.SelectedIndex = 0;
            if (PcListBox.Items.Count > 0)
            {
                PcListBox.SelectedIndex = 0;
            }
            PcListBox.ResumeLayout();

            this.Text = this.Text + $" {CpuProcessor.MethodCache.Functions.Count()}";
        }

        private void UpdateText()
        {
            if (PcListBox.SelectedItem != null)
            {
                var PCItem = (PCItem)PcListBox.SelectedItem;
                var Func = PCItem.FunctionCache;
                var MinPC = Func.MinPC;
                var MaxPC = Func.MaxPC;
                var Memory = CpuProcessor.Memory;
                AstNodeStm Node = null;
                if (Func.AstNode != null) Node = Func.AstNode.Optimize(CpuProcessor);

                var InfoLines = new List<string>();

                InfoLines.Add(String.Format("Name: {0}", Func.Name));
                InfoLines.Add(String.Format("DisableOptimizations: {0}", Func.DisableOptimizations));

                InfoLines.Add(String.Format("EntryPC: 0x{0:X8}", Func.EntryPC));
                InfoLines.Add(String.Format("MinPC: 0x{0:X8}", Func.MinPC));
                InfoLines.Add(String.Format("MaxPC: 0x{0:X8}", Func.MaxPC));
                InfoLines.Add(String.Format("TimeAnalyzeBranches: {0}", Func.TimeAnalyzeBranches.TotalMilliseconds));
                InfoLines.Add(String.Format("TimeGenerateAst: {0}", Func.TimeGenerateAst.TotalMilliseconds));
                InfoLines.Add(String.Format("TimeOptimize: {0}", Func.TimeOptimize.TotalMilliseconds));
                InfoLines.Add(String.Format("TimeGenerateIL: {0}", Func.TimeGenerateIL.TotalMilliseconds));
                InfoLines.Add(String.Format("TimeCreateDelegate: {0}", Func.TimeCreateDelegate.TotalMilliseconds));
                InfoLines.Add(String.Format("TimeLinking: {0}", Func.TimeLinking.TotalMilliseconds));
                InfoLines.Add(String.Format("TimeTotal: {0}", Func.TimeTotal.TotalMilliseconds));

                InfoLines.Add(String.Format(""));
                foreach (var Item in Func.InstructionStats.OrderBy(Pair => Pair.Value))
                {
                    InfoLines.Add(String.Format("{0}: {1}", Item.Key, Item.Value));
                }

                InfoTextBox.Text = String.Join("\r\n", InfoLines);

                var OutString = "";
                switch (LanguageComboBox.SelectedItem.ToString())
                {
                    case "C#":
                        if (Node != null)
                        {
                            OutString = Node.ToCSharpString().Replace("CpuThreadState.", "");
                        }
                        break;
                    case "IL":
                        if (Node != null)
                        {
                            OutString = Node.ToILString<Action<CpuThreadState>>();
                        }
                        break;
                    case "Ast":
                        if (Node != null)
                        {
                            OutString = AstSerializer.SerializeAsXml(Node);
                        }
                        break;
                    case "Mips":
                        {
                            var Disassembler = new MipsDisassembler();
                            try
                            {
                                for (uint PC = MinPC; PC <= MaxPC; PC += 4)
                                {
                                    var Instruction = Memory.ReadSafe<Instruction>(PC);
                                    var Result = Disassembler.Disassemble(PC, Instruction);
                                    OutString += String.Format("0x{0:X8}: {1}\r\n", PC, Result.ToString());
                                }
                            }
                            catch (Exception Exception)
                            {
                                Console.Error.WriteLine(Exception);
                            }
                        }
                        break;
                    default:
                        break;
                }

                ViewTextBox.Text = OutString.Replace("\n", "\r\n");
            }
        }

        private void PcListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateText();
        }

        private void LanguageComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateText();
        }

        private void PcListBox_DrawItem_1(object sender, DrawItemEventArgs e)
        {
            e.DrawBackground();
            e.DrawFocusRectangle();

            if (e.Index >= 0)
            {
                var ListBox = (ListBox)sender;
                var item = ListBox.Items[e.Index] as PCItem;

                var Color = item.ItemColor;

                if (item == ListBox.SelectedItem)
                {
                    //Color = SystemColors.HighlightText;
                }

                e.Graphics.DrawString(
                    item.ToString(),
                    ListBox.Font,
                    new SolidBrush(Color),
                    e.Bounds
                );
            }
        }

        private void ViewTextBox_TextChanged(object sender, EventArgs e)
        {

        }

        private void ViewTextBox_TextChanged_1(object sender, EventArgs e)
        {

        }

        private void tableLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {

        }
    }
}
