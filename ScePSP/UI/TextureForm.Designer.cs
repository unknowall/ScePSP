namespace ScePSP.UI
{
	partial class TextureForm
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
            TextureList = new System.Windows.Forms.ListBox();
            TextureViewContainer = new System.Windows.Forms.Panel();
            TextureView = new System.Windows.Forms.PictureBox();
            tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            TextureInfo = new System.Windows.Forms.TextBox();
            flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            SaveButton = new System.Windows.Forms.Button();
            tableLayoutPanel1.SuspendLayout();
            TextureViewContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)TextureView).BeginInit();
            tableLayoutPanel2.SuspendLayout();
            flowLayoutPanel1.SuspendLayout();
            SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 3;
            tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 187F));
            tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 233F));
            tableLayoutPanel1.Controls.Add(TextureList, 0, 0);
            tableLayoutPanel1.Controls.Add(TextureViewContainer, 1, 0);
            tableLayoutPanel1.Controls.Add(tableLayoutPanel2, 2, 0);
            tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(4);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 1;
            tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            tableLayoutPanel1.Size = new System.Drawing.Size(984, 503);
            tableLayoutPanel1.TabIndex = 0;
            // 
            // TextureList
            // 
            TextureList.Dock = System.Windows.Forms.DockStyle.Fill;
            TextureList.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            TextureList.FormattingEnabled = true;
            TextureList.ItemHeight = 14;
            TextureList.Location = new System.Drawing.Point(4, 4);
            TextureList.Margin = new System.Windows.Forms.Padding(4);
            TextureList.Name = "TextureList";
            TextureList.Size = new System.Drawing.Size(179, 495);
            TextureList.TabIndex = 1;
            TextureList.SelectedIndexChanged += TextureList_SelectedIndexChanged;
            // 
            // TextureViewContainer
            // 
            TextureViewContainer.AutoScroll = true;
            TextureViewContainer.BackColor = System.Drawing.Color.Transparent;
            TextureViewContainer.Controls.Add(TextureView);
            TextureViewContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            TextureViewContainer.Location = new System.Drawing.Point(191, 4);
            TextureViewContainer.Margin = new System.Windows.Forms.Padding(4);
            TextureViewContainer.Name = "TextureViewContainer";
            TextureViewContainer.Size = new System.Drawing.Size(556, 495);
            TextureViewContainer.TabIndex = 3;
            // 
            // TextureView
            // 
            TextureView.Location = new System.Drawing.Point(0, 0);
            TextureView.Margin = new System.Windows.Forms.Padding(4);
            TextureView.Name = "TextureView";
            TextureView.Size = new System.Drawing.Size(149, 167);
            TextureView.TabIndex = 0;
            TextureView.TabStop = false;
            // 
            // tableLayoutPanel2
            // 
            tableLayoutPanel2.ColumnCount = 1;
            tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            tableLayoutPanel2.Controls.Add(TextureInfo, 0, 1);
            tableLayoutPanel2.Controls.Add(flowLayoutPanel1, 0, 0);
            tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            tableLayoutPanel2.Location = new System.Drawing.Point(755, 4);
            tableLayoutPanel2.Margin = new System.Windows.Forms.Padding(4);
            tableLayoutPanel2.Name = "tableLayoutPanel2";
            tableLayoutPanel2.RowCount = 2;
            tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 42F));
            tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
            tableLayoutPanel2.Size = new System.Drawing.Size(225, 495);
            tableLayoutPanel2.TabIndex = 4;
            // 
            // TextureInfo
            // 
            TextureInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            TextureInfo.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            TextureInfo.Location = new System.Drawing.Point(4, 46);
            TextureInfo.Margin = new System.Windows.Forms.Padding(4);
            TextureInfo.Multiline = true;
            TextureInfo.Name = "TextureInfo";
            TextureInfo.Size = new System.Drawing.Size(217, 445);
            TextureInfo.TabIndex = 1;
            // 
            // flowLayoutPanel1
            // 
            flowLayoutPanel1.Controls.Add(SaveButton);
            flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            flowLayoutPanel1.Location = new System.Drawing.Point(4, 4);
            flowLayoutPanel1.Margin = new System.Windows.Forms.Padding(4);
            flowLayoutPanel1.Name = "flowLayoutPanel1";
            flowLayoutPanel1.Size = new System.Drawing.Size(217, 34);
            flowLayoutPanel1.TabIndex = 2;
            // 
            // SaveButton
            // 
            SaveButton.Location = new System.Drawing.Point(4, 4);
            SaveButton.Margin = new System.Windows.Forms.Padding(4);
            SaveButton.Name = "SaveButton";
            SaveButton.Size = new System.Drawing.Size(208, 30);
            SaveButton.TabIndex = 1;
            SaveButton.Text = "&Save...";
            SaveButton.UseVisualStyleBackColor = true;
            // 
            // TextureForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(984, 503);
            Controls.Add(tableLayoutPanel1);
            Margin = new System.Windows.Forms.Padding(4);
            Name = "TextureForm";
            ShowInTaskbar = false;
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "Texture";
            FormClosing += TextureForm_FormClosing;
            Load += TextureViewerForm_Load;
            tableLayoutPanel1.ResumeLayout(false);
            TextureViewContainer.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)TextureView).EndInit();
            tableLayoutPanel2.ResumeLayout(false);
            tableLayoutPanel2.PerformLayout();
            flowLayoutPanel1.ResumeLayout(false);
            ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
		private System.Windows.Forms.ListBox TextureList;
		private System.Windows.Forms.Panel TextureViewContainer;
		private System.Windows.Forms.PictureBox TextureView;
		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
		private System.Windows.Forms.TextBox TextureInfo;
		private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.Button SaveButton;
    }
}