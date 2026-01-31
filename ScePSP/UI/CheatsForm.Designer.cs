using System.Windows.Forms;

namespace ScePSP.UI
{
	public partial class CheatForm : Form
	{
		protected override void Dispose(bool disposing)
		{
			if (disposing && this.components != null)
			{
				this.components.Dispose();
			}
			base.Dispose(disposing);
		}

        private void InitializeComponent()
        {
            flowLayoutPanel1 = new FlowLayoutPanel();
            BtnCancel = new Button();
            BtnApply = new Button();
            BtnParse = new Button();
            tableLayoutPanel1 = new TableLayoutPanel();
            panel1 = new Panel();
            LvCheats = new ListView();
            EdCheatCode = new TextBox();
            flowLayoutPanel1.SuspendLayout();
            tableLayoutPanel1.SuspendLayout();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // flowLayoutPanel1
            // 
            flowLayoutPanel1.Controls.Add(BtnCancel);
            flowLayoutPanel1.Controls.Add(BtnApply);
            flowLayoutPanel1.Controls.Add(BtnParse);
            flowLayoutPanel1.Dock = DockStyle.Fill;
            flowLayoutPanel1.Location = new System.Drawing.Point(4, 514);
            flowLayoutPanel1.Margin = new Padding(4);
            flowLayoutPanel1.Name = "flowLayoutPanel1";
            flowLayoutPanel1.RightToLeft = RightToLeft.Yes;
            flowLayoutPanel1.Size = new System.Drawing.Size(719, 35);
            flowLayoutPanel1.TabIndex = 0;
            // 
            // BtnCancel
            // 
            BtnCancel.DialogResult = DialogResult.Cancel;
            BtnCancel.Location = new System.Drawing.Point(627, 4);
            BtnCancel.Margin = new Padding(4);
            BtnCancel.Name = "BtnCancel";
            BtnCancel.Size = new System.Drawing.Size(88, 30);
            BtnCancel.TabIndex = 1;
            BtnCancel.Text = "&Cancel";
            BtnCancel.UseVisualStyleBackColor = true;
            BtnCancel.Click += CancelButton_Click;
            // 
            // BtnApply
            // 
            BtnApply.DialogResult = DialogResult.OK;
            BtnApply.Location = new System.Drawing.Point(531, 4);
            BtnApply.Margin = new Padding(4);
            BtnApply.Name = "BtnApply";
            BtnApply.Size = new System.Drawing.Size(88, 30);
            BtnApply.TabIndex = 0;
            BtnApply.Text = "&Apply";
            BtnApply.UseVisualStyleBackColor = true;
            BtnApply.Click += ApplyButton_Click;
            // 
            // BtnParse
            // 
            BtnParse.DialogResult = DialogResult.OK;
            BtnParse.Location = new System.Drawing.Point(435, 4);
            BtnParse.Margin = new Padding(4);
            BtnParse.Name = "BtnParse";
            BtnParse.Size = new System.Drawing.Size(88, 30);
            BtnParse.TabIndex = 2;
            BtnParse.Text = "&Parse";
            BtnParse.UseVisualStyleBackColor = true;
            BtnParse.Click += BtnParse_Click;
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 1;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Controls.Add(flowLayoutPanel1, 0, 1);
            tableLayoutPanel1.Controls.Add(panel1, 0, 0);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            tableLayoutPanel1.Margin = new Padding(4);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 2;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 43F));
            tableLayoutPanel1.Size = new System.Drawing.Size(727, 553);
            tableLayoutPanel1.TabIndex = 0;
            tableLayoutPanel1.Paint += tableLayoutPanel1_Paint;
            // 
            // panel1
            // 
            panel1.Controls.Add(LvCheats);
            panel1.Controls.Add(EdCheatCode);
            panel1.Dock = DockStyle.Fill;
            panel1.Location = new System.Drawing.Point(3, 3);
            panel1.Name = "panel1";
            panel1.Size = new System.Drawing.Size(721, 504);
            panel1.TabIndex = 1;
            // 
            // LvCheats
            // 
            LvCheats.CheckBoxes = true;
            LvCheats.Dock = DockStyle.Left;
            LvCheats.Font = new System.Drawing.Font("Microsoft YaHei UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 134);
            LvCheats.FullRowSelect = true;
            LvCheats.HeaderStyle = ColumnHeaderStyle.None;
            LvCheats.LabelWrap = false;
            LvCheats.Location = new System.Drawing.Point(0, 0);
            LvCheats.MultiSelect = false;
            LvCheats.Name = "LvCheats";
            LvCheats.Size = new System.Drawing.Size(345, 504);
            LvCheats.TabIndex = 3;
            LvCheats.UseCompatibleStateImageBehavior = false;
            LvCheats.View = View.List;
            // 
            // EdCheatCode
            // 
            EdCheatCode.Dock = DockStyle.Right;
            EdCheatCode.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            EdCheatCode.Location = new System.Drawing.Point(367, 0);
            EdCheatCode.Margin = new Padding(4);
            EdCheatCode.Multiline = true;
            EdCheatCode.Name = "EdCheatCode";
            EdCheatCode.ScrollBars = ScrollBars.Vertical;
            EdCheatCode.Size = new System.Drawing.Size(354, 504);
            EdCheatCode.TabIndex = 2;
            // 
            // CheatForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(727, 553);
            Controls.Add(tableLayoutPanel1);
            Margin = new Padding(4);
            Name = "CheatForm";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "CheatsForm";
            Load += CheatsForm_Load;
            Shown += CheatsForm_Shown;
            flowLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.ResumeLayout(false);
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ResumeLayout(false);
        }

        private global::System.ComponentModel.IContainer components;
        private FlowLayoutPanel flowLayoutPanel1;
        private Button BtnCancel;
        private Button BtnApply;
        private TableLayoutPanel tableLayoutPanel1;
        private Panel panel1;
        private TextBox EdCheatCode;
        private ListView LvCheats;
        private Button BtnParse;
    }
}
