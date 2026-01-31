using HexBoxControl;
using ScePSP.Memory;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ScePSP.UI
{
    public partial class MemForm : Form
    {
        PspContext context;

        PspMemory Memory;

        private byte[] blankdata = new byte[1024];

        private MemorySearch memsearch;

        private static List<(int Address, object Value)> SearchResults = new List<(int Address, object Value)> { };

        public unsafe MemForm(PspContext context)
        {
            InitializeComponent();

            this.context = context;

            Memory = context.GetInstance<PspMemory>();
        }

        private unsafe void MemForm_Shown(object sender, EventArgs e)
        {
            CboView.Items.AddRange(Enum.GetNames(typeof(HexBoxViewMode)));
            CboView.SelectedIndex = 2;

            CboEncode.Items.AddRange
            (
                new object[]
                {
                    new AnsiCharConvertor(),
                    new AsciiCharConvertor(),
                    new Utf8CharConvertor()
                }
            );
            CboEncode.SelectedIndex = 0;

            HexBox.ResetOffset = false;
            HexBox.AddressOffset = PspMemory.MainOffset;
            HexBox.Edited += HexBox_Edited;

            if (context != null)
            {
                HexBox.Dump = ConvertBytePointerToByteArray((byte*)Memory.MainPtr, 32 * 1024 * 1024);
            }
            else
            {
                HexBox.Dump = blankdata;
            }

            updateml();
        }

        private unsafe byte[] ConvertBytePointerToByteArray(byte* ptr, int length)
        {
            byte[] result = new byte[length];
            Marshal.Copy((IntPtr)ptr, result, 0, length);
            return result;
        }

        private void CboView_SelectedIndexChanged(object sender, EventArgs e)
        {
            HexBoxViewMode mode;
            Enum.TryParse(CboView.SelectedItem.ToString(), out mode);
            HexBox.ViewMode = mode;
        }

        private void CboEncode_SelectedIndexChanged(object sender, EventArgs e)
        {
            HexBox.CharConverter = CboEncode.SelectedItem as ICharConverter;
        }

        private void tbgoto_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r')
                btngo_Click(sender, e);
        }

        private unsafe void btnupd_Click(object sender, EventArgs e)
        {
            if (context == null) return;

            HexBox.Dump = ConvertBytePointerToByteArray((byte*)Memory.MainPtr, 32 * 1024 * 1024);
        }

        private unsafe void HexBox_Edited(object sender, HexBoxEditEventArgs e)
        {
            if (context == null || e.NewValue == e.OldValue) return;

            Memory.Write1((uint)(PspMemory.MainOffset + e.Offset), (byte)e.NewValue);
        }

        private void btngo_Click(object sender, EventArgs e)
        {
            long pos = 0;
            try
            {
                pos = Convert.ToInt32(tbgoto.Text, 16);
            }
            catch
            {
                return;
            }

            if (pos < PspMemory.MainOffset)
                pos = pos + PspMemory.MainOffset;

            HexBox.ScrollTo(pos);
        }

        private void ml_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            //switch (this.ml.Columns[e.ColumnIndex].Name)
            //{
            //    case "address":
            //        e.Value = (PSX_BASE + SearchResults[e.RowIndex].Address).ToString("X8");
            //        break;

            //    case "val":
            //        e.Value = SearchResults[e.RowIndex].Value;
            //        break;
            //}
        }

        private void ml_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex > SearchResults.Count)
                return;

            long pos = SearchResults[e.RowIndex].Address + PspMemory.MainOffset;

            HexBox.ScrollTo(pos);
        }

        private unsafe void ml_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (this.ml.Columns[e.ColumnIndex].Name == "val")
            {

                SearchResults[e.RowIndex] = (SearchResults[e.RowIndex].Address, this.ml.Rows[e.RowIndex].Cells[1].Value);

                uint tmp = uint.Parse(SearchResults[e.RowIndex].Value.ToString());
                uint adr = (uint)SearchResults[e.RowIndex].Address;

                if (tmp < 0xFF)
                {
                    Memory.Write1(adr, (byte)tmp);
                }
                else if (tmp < 0xFFFF)
                {
                    Memory.Write2(adr, (ushort)tmp);
                }
                else
                {
                    Memory.Write4(adr, tmp);
                }
            }
        }

        private void findb_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '.')
            {
                e.Handled = true;
            }
        }

        private unsafe void btnr_Click(object sender, EventArgs e)
        {
            if (context == null) return;

            memsearch = new MemorySearch((byte*)Memory.MainPtr, 32 * 1024 * 1024);

            SearchResults.Clear();

            updateml();
        }

        private unsafe void btns_Click(object sender, EventArgs e)
        {
            if (context == null) return;

            if (memsearch == null)
                memsearch = new MemorySearch((byte*)Memory.MainPtr, 32 * 1024 * 1024);
            else
                memsearch.UpdateData((byte*)Memory.MainPtr, 32 * 1024 * 1024);

            if (rbbyte.Checked)
            {
                byte tmp;
                if (!byte.TryParse(findb.Text, out tmp))
                    return;
                memsearch.SearchByte(tmp);
            }
            else
            if (rbWord.Checked)
            {
                ushort tmp;
                if (!ushort.TryParse(findb.Text, out tmp))
                    return;
                memsearch.SearchWord(tmp);
            }
            else
            if (rbDword.Checked)
            {
                uint tmp;
                if (!uint.TryParse(findb.Text, out tmp))
                    return;
                memsearch.SearchDword(tmp);
            }
            else
            if (rbfloat.Checked)
            {
                float tmp;
                if (!float.TryParse(findb.Text, out tmp))
                    return;
                memsearch.SearchFloat(tmp);
            }

            SearchResults = memsearch.GetResults();

            updateml();
        }

        private void updateml()
        {
            labse.Text = $"Found {SearchResults.Count}";
            ml.Rows.Clear();
            for (int i = 0; i < SearchResults.Count; i++)
            {
                if (i >= 500) break;

                ml.Rows.Add((PspMemory.MainOffset + SearchResults[i].Address).ToString("X8"), SearchResults[i].Value);
            }
        }
    }
}
