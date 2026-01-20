using ScePSP.BackEnd.OpenGL;
using ScePSP.Cpu;
using ScePSP.GE;
using ScePSP.Types;
using ScePSPUtils;
using ScePSPUtils.Drawing;
using ScePSPUtils.Drawing.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ScePSP.UI
{
    public partial class TextureForm : Form
    {
        GLBackEnd backend;

        public TextureForm()
        {
            InitializeComponent();

            backend = (PSPDrivers.GeBackEnd as GLBackEnd);
        }

        public class TextureElement
        {
            public TTexture Texture;

            public TextureElement(TTexture Texture)
            {
                this.Texture = Texture;
            }

            public override string ToString()
            {
                return String.Format("{0:X16}", this.Texture.Info.TextureHash);
            }
        }

        private void UpdateTextureList()
        {
            TextureList.SuspendLayout();
            try
            {
                TextureList.Items.Clear();
                foreach (var Element in backend.TextureCache.Cache.Values)
                {
                    TextureList.Items.Add(new TextureElement(Element));
                }
            }
            finally
            {
                TextureList.ResumeLayout();
            }
        }

        private void TextureViewerForm_Load(object sender, EventArgs e)
        {
            GLBackEnd.Context.MakeCurrent();

            UpdateTextureList();
            if (TextureList.Items.Count > 0)
            {
                TextureList.SelectedIndex = 0;
            }

            Text = $"Total Cached Texture: {TextureList.Items.Count}";
        }

        private void TextureForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            GLBackEnd.Context.ReleaseCurrent();
        }

        private void TextureView_Paint(object sender, PaintEventArgs e)
        {

        }

        private void UpdateTexture()
        {
            var Item = (TextureElement)TextureList.SelectedItem;
            var Info = Item.Texture.Info;
            var Texture = Item.Texture;
            byte[] PixelsData = Item.Texture.ReadPixels();

            TextureView.Image = new Bitmap(Texture.Width, Texture.Height).SetChannelsDataInterleaved(
                PixelsData,
                BitmapChannelList.Rgba
            );
            TextureView.Size = new System.Drawing.Size(Texture.Width, Texture.Height);

            var InfoLines = new List<string>();
            InfoLines.Add(String.Format("Active Hit: {0}", Texture.Hit));
            InfoLines.Add(String.Format("Scale Mode: {0}", Texture.Info.ScaleMode));
            InfoLines.Add(String.Format("Scale: {0}", Texture.Info.ScaleX));
            InfoLines.Add("");

            InfoLines.Add(String.Format("--Texture--"));
            InfoLines.Add(String.Format("Hash: 0x{0:X16}", Info.TextureHash));
            InfoLines.Add(String.Format("Address: 0x{0:X8}", Info.TextureAddress));
            InfoLines.Add(String.Format("Format: {0}", Info.TextureFormat));
            InfoLines.Add(String.Format("Size: {0}x{1}", Texture.Width, Texture.Height));
            InfoLines.Add(String.Format("Swizzled: {0}", Info.Swizzled));
            InfoLines.Add("");

            InfoLines.Add(String.Format("--ColorTest--"));
            InfoLines.Add(String.Format("Enabled: {0}", Info.ColorTestEnabled));
            InfoLines.Add(String.Format("Mask: {0}", Info.ColorTestMask));
            InfoLines.Add(String.Format("Function: {0}", Info.ColorTestFunction));
            InfoLines.Add(String.Format("Ref: {0}", Info.ColorTestRef));
            InfoLines.Add("");

            InfoLines.Add(String.Format("--Clut--"));
            InfoLines.Add(String.Format("Hash: 0x{0:X16}", Info.ClutHash));
            InfoLines.Add(String.Format("Address: 0x{0:X8}", Info.ClutAddress));
            InfoLines.Add(String.Format("Format: {0}", Info.ClutFormat));
            InfoLines.Add(String.Format("Mask: {0}", Info.ClutMask));
            InfoLines.Add(String.Format("Shift: {0}", Info.ClutShift));
            InfoLines.Add(String.Format("Start: {0}", Info.ClutStart));

            TextureInfo.Text = String.Join("\r\n", InfoLines);
        }

        private void TextureList_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateTexture();
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            var Item = (TextureElement)TextureList.SelectedItem;
            var SaveFileDialog = new SaveFileDialog();
            SaveFileDialog.DefaultExt = "png";
            SaveFileDialog.AddExtension = true;
            SaveFileDialog.FileName = Item.ToString();
            SaveFileDialog.Filter = "Png Image (.png)|*.png";
            if (SaveFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                TextureView.Image.Save(SaveFileDialog.FileName);
            }
        }
    }
}
