using ScePSP.Core;
using System;
using System.Windows.Forms;

namespace ScePSP.UI
{
    public partial class SetForm : Form
    {
        PspStoredConfig cfg;

        public SetForm(PspStoredConfig cfg)
        {
            InitializeComponent();

            this.cfg = cfg;
        }

        private void SetForm_Shown(object sender, EventArgs e)
        {
            LabIRRes.Text = $"( {480 * NumIR.Value} x {272 * NumIR.Value} )";

            NumIR.Value = cfg.RenderScale;
            NumAudio.Value = cfg.AuPlayBackMs;
            NumAT3.Value = cfg.AuDecodeBuf;
            NumH264.Value = cfg.AvDecodeBuf;
            NumTexCache.Value = cfg.MaxTexCache;
            NumTexHit.Value = cfg.TexMinHits;
            NumTexScale.Value = cfg.TexScaleX;
            NumDepth.Value = cfg.ScanFunctionsDepth;
            ChkDyn.Checked = cfg.ScanCreatingFunctions;
            ChkDynL.Checked = cfg.DepthOpt;
            ChkDynTick.Checked = cfg.TickCall;
            ChkTail.Checked = cfg.TailCall;
            ChkFastMem.Checked = cfg.FastMemoryReader;
            CbScaleMode.SelectedIndex = cfg.TexScaleType + 1;
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void BtnApply_Click(object sender, EventArgs e)
        {
            cfg.RenderScale = (int)NumIR.Value;
            cfg.AuPlayBackMs = (int)NumAudio.Value;
            cfg.AuDecodeBuf = (int)NumAT3.Value;
            cfg.AvDecodeBuf = (int)NumH264.Value;
            cfg.MaxTexCache = (int)NumTexCache.Value;
            cfg.TexMinHits = (int)NumTexHit.Value;
            cfg.TexScaleX = (int)NumTexScale.Value;
            cfg.ScanFunctionsDepth = (int)NumDepth.Value;
            cfg.ScanCreatingFunctions = ChkDyn.Checked;
            cfg.DepthOpt = ChkDynL.Checked;
            cfg.TickCall = ChkDynTick.Checked;
            cfg.TailCall = ChkTail.Checked;
            cfg.FastMemoryReader = ChkFastMem.Checked;
            cfg.TexScaleType = CbScaleMode.SelectedIndex - 1;
            cfg.Save();
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            LabIRRes.Text = $"( {480 * NumIR.Value} x {272 * NumIR.Value} )";
        }
    }
}
