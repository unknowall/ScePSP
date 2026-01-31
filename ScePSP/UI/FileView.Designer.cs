namespace ScePSP.UI
{
    partial class FileExtract
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
            EdFile = new System.Windows.Forms.TextBox();
            btnExtract = new System.Windows.Forms.Button();
            SuspendLayout();
            // 
            // EdFile
            // 
            EdFile.Location = new System.Drawing.Point(12, 12);
            EdFile.Name = "EdFile";
            EdFile.Size = new System.Drawing.Size(392, 23);
            EdFile.TabIndex = 1;
            // 
            // btnExtract
            // 
            btnExtract.Location = new System.Drawing.Point(410, 12);
            btnExtract.Name = "btnExtract";
            btnExtract.Size = new System.Drawing.Size(88, 23);
            btnExtract.TabIndex = 2;
            btnExtract.Text = "Extract";
            btnExtract.UseVisualStyleBackColor = true;
            btnExtract.Click += btnExtract_Click;
            // 
            // FileExtract
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(510, 46);
            Controls.Add(btnExtract);
            Controls.Add(EdFile);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Name = "FileExtract";
            Text = "File Extract";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private System.Windows.Forms.TextBox EdFile;
        private System.Windows.Forms.Button btnExtract;
    }
}