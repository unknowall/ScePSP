namespace ScePSP.UI
{
    partial class FrmAbout
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
            LabTitle = new System.Windows.Forms.Label();
            label2 = new System.Windows.Forms.Label();
            textBox1 = new System.Windows.Forms.TextBox();
            LabText = new System.Windows.Forms.Label();
            LabLink = new System.Windows.Forms.LinkLabel();
            SupportLink = new System.Windows.Forms.LinkLabel();
            labSupport = new System.Windows.Forms.Label();
            SuspendLayout();
            // 
            // LabTitle
            // 
            LabTitle.AutoSize = true;
            LabTitle.Font = new System.Drawing.Font("Microsoft YaHei UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 134);
            LabTitle.Location = new System.Drawing.Point(26, 21);
            LabTitle.Name = "LabTitle";
            LabTitle.Size = new System.Drawing.Size(166, 22);
            LabTitle.TabIndex = 0;
            LabTitle.Text = "ScePSP Alpha 0.0.1";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(26, 179);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(70, 17);
            label2.TabIndex = 1;
            label2.Text = "Maintainer";
            // 
            // textBox1
            // 
            textBox1.Location = new System.Drawing.Point(34, 199);
            textBox1.Multiline = true;
            textBox1.Name = "textBox1";
            textBox1.ReadOnly = true;
            textBox1.Size = new System.Drawing.Size(254, 44);
            textBox1.TabIndex = 2;
            textBox1.Text = "unknowall - sgfree@hotmail.com";
            // 
            // LabText
            // 
            LabText.Font = new System.Drawing.Font("Microsoft YaHei UI", 10.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 134);
            LabText.Location = new System.Drawing.Point(34, 53);
            LabText.Name = "LabText";
            LabText.Size = new System.Drawing.Size(254, 56);
            LabText.TabIndex = 3;
            LabText.Text = "A Lightweight PlayStation Portable Emulator Fully Developed in C#";
            // 
            // LabLink
            // 
            LabLink.AutoSize = true;
            LabLink.Location = new System.Drawing.Point(35, 111);
            LabLink.Name = "LabLink";
            LabLink.Size = new System.Drawing.Size(224, 17);
            LabLink.TabIndex = 4;
            LabLink.TabStop = true;
            LabLink.Text = "https://github.com/unknowall/ScePSP";
            LabLink.LinkClicked += Link_LinkClicked;
            // 
            // SupportLink
            // 
            SupportLink.AutoSize = true;
            SupportLink.Location = new System.Drawing.Point(35, 156);
            SupportLink.Name = "SupportLink";
            SupportLink.Size = new System.Drawing.Size(168, 17);
            SupportLink.TabIndex = 5;
            SupportLink.TabStop = true;
            SupportLink.Text = "https://ko-fi.com/unknowall";
            SupportLink.LinkClicked += SupportLink_Click;
            // 
            // labSupport
            // 
            labSupport.AutoSize = true;
            labSupport.Location = new System.Drawing.Point(35, 135);
            labSupport.Name = "labSupport";
            labSupport.Size = new System.Drawing.Size(188, 17);
            labSupport.TabIndex = 6;
            labSupport.Text = "如果您愿意，可以请我喝一杯咖啡";
            // 
            // FrmAbout
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(318, 251);
            Controls.Add(labSupport);
            Controls.Add(SupportLink);
            Controls.Add(LabLink);
            Controls.Add(LabText);
            Controls.Add(textBox1);
            Controls.Add(label2);
            Controls.Add(LabTitle);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Name = "FrmAbout";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "About";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label LabTitle;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Label LabText;
        private System.Windows.Forms.LinkLabel LabLink;
        private System.Windows.Forms.LinkLabel SupportLink;
        private System.Windows.Forms.Label labSupport;
    }
}
