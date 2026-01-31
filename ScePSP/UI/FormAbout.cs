using System.Windows.Forms;

namespace ScePSP.UI
{
    public partial class FrmAbout : Form
    {
        public FrmAbout()
        {
            InitializeComponent();

            LabTitle.Text = MainForm.Title_O;

            //LabText.Text = "A Lightweight PlayStation Portable Emulator Fully Developed in C#";

            //labSupport.Text = "Support: ";
        }

        private void Link_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = LabLink.Text,
                    UseShellExecute = true
                });
            }
            catch
            {

            }
        }

        private void SupportLink_Click(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = SupportLink.Text,
                    UseShellExecute = true
                });
            }
            catch
            {

            }
        }
    }
}
