namespace ScePSP.UI
{
	partial class FuncForm
    {
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            splitContainer1 = new System.Windows.Forms.SplitContainer();
            EdFind = new System.Windows.Forms.TextBox();
            BtnFind = new System.Windows.Forms.Button();
            PcListBox = new System.Windows.Forms.ListBox();
            splitContainer2 = new System.Windows.Forms.SplitContainer();
            ViewTextBox = new System.Windows.Forms.TextBox();
            InfoTextBox = new System.Windows.Forms.TextBox();
            LanguageComboBox = new System.Windows.Forms.ComboBox();
            tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer2).BeginInit();
            splitContainer2.Panel1.SuspendLayout();
            splitContainer2.Panel2.SuspendLayout();
            splitContainer2.SuspendLayout();
            SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 1;
            tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            tableLayoutPanel1.Controls.Add(splitContainer1, 0, 0);
            tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(4);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 3;
            tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 9F));
            tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 23F));
            tableLayoutPanel1.Size = new System.Drawing.Size(1092, 640);
            tableLayoutPanel1.TabIndex = 4;
            tableLayoutPanel1.Paint += tableLayoutPanel1_Paint;
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            splitContainer1.Location = new System.Drawing.Point(0, 0);
            splitContainer1.Margin = new System.Windows.Forms.Padding(0);
            splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(EdFind);
            splitContainer1.Panel1.Controls.Add(BtnFind);
            splitContainer1.Panel1.Controls.Add(PcListBox);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(splitContainer2);
            splitContainer1.Panel2.Controls.Add(LanguageComboBox);
            splitContainer1.Size = new System.Drawing.Size(1092, 608);
            splitContainer1.SplitterDistance = 228;
            splitContainer1.SplitterWidth = 5;
            splitContainer1.TabIndex = 4;
            // 
            // EdFind
            // 
            EdFind.Location = new System.Drawing.Point(12, 3);
            EdFind.Name = "EdFind";
            EdFind.Size = new System.Drawing.Size(139, 23);
            EdFind.TabIndex = 3;
            // 
            // BtnFind
            // 
            BtnFind.Location = new System.Drawing.Point(157, 3);
            BtnFind.Name = "BtnFind";
            BtnFind.Size = new System.Drawing.Size(68, 23);
            BtnFind.TabIndex = 2;
            BtnFind.Text = "Find";
            BtnFind.UseVisualStyleBackColor = true;
            BtnFind.Click += BtnGO_Click;
            // 
            // PcListBox
            // 
            PcListBox.Dock = System.Windows.Forms.DockStyle.Bottom;
            PcListBox.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            PcListBox.FormattingEnabled = true;
            PcListBox.Location = new System.Drawing.Point(0, 28);
            PcListBox.Margin = new System.Windows.Forms.Padding(4);
            PcListBox.Name = "PcListBox";
            PcListBox.Size = new System.Drawing.Size(228, 580);
            PcListBox.TabIndex = 1;
            PcListBox.DrawItem += PcListBox_DrawItem_1;
            PcListBox.SelectedIndexChanged += PcListBox_SelectedIndexChanged;
            // 
            // splitContainer2
            // 
            splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            splitContainer2.Location = new System.Drawing.Point(0, 25);
            splitContainer2.Margin = new System.Windows.Forms.Padding(4);
            splitContainer2.Name = "splitContainer2";
            // 
            // splitContainer2.Panel1
            // 
            splitContainer2.Panel1.Controls.Add(ViewTextBox);
            // 
            // splitContainer2.Panel2
            // 
            splitContainer2.Panel2.Controls.Add(InfoTextBox);
            splitContainer2.Size = new System.Drawing.Size(859, 583);
            splitContainer2.SplitterDistance = 622;
            splitContainer2.SplitterWidth = 5;
            splitContainer2.TabIndex = 9;
            // 
            // ViewTextBox
            // 
            ViewTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            ViewTextBox.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            ViewTextBox.Location = new System.Drawing.Point(0, 0);
            ViewTextBox.Margin = new System.Windows.Forms.Padding(4);
            ViewTextBox.Multiline = true;
            ViewTextBox.Name = "ViewTextBox";
            ViewTextBox.ReadOnly = true;
            ViewTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            ViewTextBox.Size = new System.Drawing.Size(622, 583);
            ViewTextBox.TabIndex = 9;
            ViewTextBox.WordWrap = false;
            // 
            // InfoTextBox
            // 
            InfoTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            InfoTextBox.Font = new System.Drawing.Font("Consolas", 8.25F);
            InfoTextBox.Location = new System.Drawing.Point(0, 0);
            InfoTextBox.Margin = new System.Windows.Forms.Padding(4);
            InfoTextBox.Multiline = true;
            InfoTextBox.Name = "InfoTextBox";
            InfoTextBox.Size = new System.Drawing.Size(232, 583);
            InfoTextBox.TabIndex = 6;
            // 
            // LanguageComboBox
            // 
            LanguageComboBox.Dock = System.Windows.Forms.DockStyle.Top;
            LanguageComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            LanguageComboBox.FormattingEnabled = true;
            LanguageComboBox.Items.AddRange(new object[] { "C#", "Ast", "IL", "Mips" });
            LanguageComboBox.Location = new System.Drawing.Point(0, 0);
            LanguageComboBox.Margin = new System.Windows.Forms.Padding(4);
            LanguageComboBox.Name = "LanguageComboBox";
            LanguageComboBox.Size = new System.Drawing.Size(859, 25);
            LanguageComboBox.TabIndex = 3;
            LanguageComboBox.SelectedIndexChanged += LanguageComboBox_SelectedIndexChanged;
            // 
            // FuncForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1092, 640);
            Controls.Add(tableLayoutPanel1);
            Margin = new System.Windows.Forms.Padding(4);
            Name = "FuncForm";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "Function";
            Shown += FunctionForm_Shown;
            tableLayoutPanel1.ResumeLayout(false);
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel1.PerformLayout();
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            splitContainer2.Panel1.ResumeLayout(false);
            splitContainer2.Panel1.PerformLayout();
            splitContainer2.Panel2.ResumeLayout(false);
            splitContainer2.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer2).EndInit();
            splitContainer2.ResumeLayout(false);
            ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
		private System.Windows.Forms.SplitContainer splitContainer1;
		private System.Windows.Forms.ListBox PcListBox;
		private System.Windows.Forms.ComboBox LanguageComboBox;
		private System.Windows.Forms.SplitContainer splitContainer2;
		private System.Windows.Forms.TextBox ViewTextBox;
		private System.Windows.Forms.TextBox InfoTextBox;
        private System.Windows.Forms.TextBox EdFind;
        private System.Windows.Forms.Button BtnFind;
    }
}