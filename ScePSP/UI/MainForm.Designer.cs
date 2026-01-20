namespace ScePSP.UI
{
    partial class MainForm
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
            menuStrip1 = new System.Windows.Forms.MenuStrip();
            MnuFile = new System.Windows.Forms.ToolStripMenuItem();
            MnuLoad = new System.Windows.Forms.ToolStripMenuItem();
            toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            MnuSetPath = new System.Windows.Forms.ToolStripMenuItem();
            toolStripMenuItem4 = new System.Windows.Forms.ToolStripSeparator();
            MnuKeyConfig = new System.Windows.Forms.ToolStripMenuItem();
            MnuFullSpeed = new System.Windows.Forms.ToolStripMenuItem();
            toolStripMenuItem5 = new System.Windows.Forms.ToolStripSeparator();
            MnuExit = new System.Windows.Forms.ToolStripMenuItem();
            MnuDebug = new System.Windows.Forms.ToolStripMenuItem();
            MnuFunc = new System.Windows.Forms.ToolStripMenuItem();
            toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
            MnuTexture = new System.Windows.Forms.ToolStripMenuItem();
            toolStripMenuItem3 = new System.Windows.Forms.ToolStripSeparator();
            DbgHleCall = new System.Windows.Forms.ToolStripMenuItem();
            MnuH264 = new System.Windows.Forms.ToolStripMenuItem();
            MnuOpenCfg = new System.Windows.Forms.ToolStripMenuItem();
            helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            mnugithub = new System.Windows.Forms.ToolStripMenuItem();
            mnugithublink = new System.Windows.Forms.ToolStripMenuItem();
            panel1 = new System.Windows.Forms.Panel();
            menuStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // menuStrip1
            // 
            menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { MnuFile, MnuDebug, helpToolStripMenuItem });
            menuStrip1.Location = new System.Drawing.Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new System.Drawing.Size(862, 25);
            menuStrip1.TabIndex = 0;
            menuStrip1.Text = "menuStrip1";
            // 
            // MnuFile
            // 
            MnuFile.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { MnuLoad, toolStripMenuItem1, MnuSetPath, toolStripMenuItem4, MnuKeyConfig, MnuFullSpeed, toolStripMenuItem5, MnuExit });
            MnuFile.Name = "MnuFile";
            MnuFile.Size = new System.Drawing.Size(39, 21);
            MnuFile.Text = "File";
            // 
            // MnuLoad
            // 
            MnuLoad.Name = "MnuLoad";
            MnuLoad.Size = new System.Drawing.Size(219, 22);
            MnuLoad.Text = "Load ROM";
            MnuLoad.Click += MnuLoad_Click;
            // 
            // toolStripMenuItem1
            // 
            toolStripMenuItem1.Name = "toolStripMenuItem1";
            toolStripMenuItem1.Size = new System.Drawing.Size(216, 6);
            // 
            // MnuSetPath
            // 
            MnuSetPath.Name = "MnuSetPath";
            MnuSetPath.Size = new System.Drawing.Size(219, 22);
            MnuSetPath.Text = "Set Rom Path";
            MnuSetPath.Click += MnuSetPath_Click;
            // 
            // toolStripMenuItem4
            // 
            toolStripMenuItem4.Name = "toolStripMenuItem4";
            toolStripMenuItem4.Size = new System.Drawing.Size(216, 6);
            // 
            // MnuKeyConfig
            // 
            MnuKeyConfig.Name = "MnuKeyConfig";
            MnuKeyConfig.Size = new System.Drawing.Size(219, 22);
            MnuKeyConfig.Text = "Key Config";
            MnuKeyConfig.Click += MnuKeyConfig_Click;
            // 
            // MnuFullSpeed
            // 
            MnuFullSpeed.Enabled = false;
            MnuFullSpeed.Name = "MnuFullSpeed";
            MnuFullSpeed.Size = new System.Drawing.Size(219, 22);
            MnuFullSpeed.Text = "Press TAB for Full Speed";
            // 
            // toolStripMenuItem5
            // 
            toolStripMenuItem5.Name = "toolStripMenuItem5";
            toolStripMenuItem5.Size = new System.Drawing.Size(216, 6);
            // 
            // MnuExit
            // 
            MnuExit.Name = "MnuExit";
            MnuExit.Size = new System.Drawing.Size(219, 22);
            MnuExit.Text = "Exit Game";
            MnuExit.Click += MnuExit_Click;
            // 
            // MnuDebug
            // 
            MnuDebug.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { MnuFunc, toolStripMenuItem2, MnuTexture, toolStripMenuItem3, DbgHleCall, MnuH264, MnuOpenCfg });
            MnuDebug.Name = "MnuDebug";
            MnuDebug.Size = new System.Drawing.Size(59, 21);
            MnuDebug.Text = "Debug";
            // 
            // MnuFunc
            // 
            MnuFunc.Name = "MnuFunc";
            MnuFunc.Size = new System.Drawing.Size(173, 22);
            MnuFunc.Text = "FunctionView";
            MnuFunc.Click += MnuFunc_Click;
            // 
            // toolStripMenuItem2
            // 
            toolStripMenuItem2.Name = "toolStripMenuItem2";
            toolStripMenuItem2.Size = new System.Drawing.Size(170, 6);
            // 
            // MnuTexture
            // 
            MnuTexture.Name = "MnuTexture";
            MnuTexture.Size = new System.Drawing.Size(173, 22);
            MnuTexture.Text = "TextureView";
            MnuTexture.Click += MnuTexture_Click;
            // 
            // toolStripMenuItem3
            // 
            toolStripMenuItem3.Name = "toolStripMenuItem3";
            toolStripMenuItem3.Size = new System.Drawing.Size(170, 6);
            // 
            // DbgHleCall
            // 
            DbgHleCall.CheckOnClick = true;
            DbgHleCall.Name = "DbgHleCall";
            DbgHleCall.Size = new System.Drawing.Size(173, 22);
            DbgHleCall.Text = "Track HLE Calls";
            DbgHleCall.Click += DbgHleCall_Click;
            // 
            // MnuH264
            // 
            MnuH264.CheckOnClick = true;
            MnuH264.Name = "MnuH264";
            MnuH264.Size = new System.Drawing.Size(173, 22);
            MnuH264.Text = "H264 Enable";
            // 
            // MnuOpenCfg
            // 
            MnuOpenCfg.Name = "MnuOpenCfg";
            MnuOpenCfg.Size = new System.Drawing.Size(173, 22);
            MnuOpenCfg.Text = "Open Config.xml";
            MnuOpenCfg.Click += MnuOpenCfg_Click;
            // 
            // helpToolStripMenuItem
            // 
            helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { mnugithub, mnugithublink });
            helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            helpToolStripMenuItem.Size = new System.Drawing.Size(55, 21);
            helpToolStripMenuItem.Text = "About";
            // 
            // mnugithub
            // 
            mnugithub.Enabled = false;
            mnugithub.Name = "mnugithub";
            mnugithub.Size = new System.Drawing.Size(292, 22);
            mnugithub.Text = "Github:";
            // 
            // mnugithublink
            // 
            mnugithublink.Name = "mnugithublink";
            mnugithublink.Size = new System.Drawing.Size(292, 22);
            mnugithublink.Text = "https://github.com/unknowall/ScePSP";
            mnugithublink.Click += mnugithublink_Click;
            // 
            // panel1
            // 
            panel1.BackColor = System.Drawing.SystemColors.ControlDarkDark;
            panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            panel1.Location = new System.Drawing.Point(0, 25);
            panel1.Name = "panel1";
            panel1.Size = new System.Drawing.Size(862, 534);
            panel1.TabIndex = 1;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(862, 559);
            Controls.Add(panel1);
            Controls.Add(menuStrip1);
            MainMenuStrip = menuStrip1;
            Name = "MainForm";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "ScePSP";
            FormClosing += MainForm_FormClosing;
            Load += MainForm_Load;
            Shown += MainForm_Shown;
            KeyDown += MainForm_KeyDown;
            KeyUp += MainForm_KeyUp;
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem MnuFile;
        private System.Windows.Forms.ToolStripMenuItem MnuLoad;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem MnuExit;
        private System.Windows.Forms.ToolStripMenuItem MnuDebug;
        private System.Windows.Forms.ToolStripMenuItem MnuFunc;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem2;
        private System.Windows.Forms.ToolStripMenuItem MnuTexture;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem3;
        private System.Windows.Forms.ToolStripMenuItem DbgHleCall;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.ToolStripMenuItem MnuSetPath;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem4;
        private System.Windows.Forms.ToolStripMenuItem MnuKeyConfig;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem5;
        private System.Windows.Forms.ToolStripMenuItem MnuFullSpeed;
        private System.Windows.Forms.ToolStripMenuItem MnuH264;
        private System.Windows.Forms.ToolStripMenuItem MnuOpenCfg;
        private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem mnugithub;
        private System.Windows.Forms.ToolStripMenuItem mnugithublink;
    }
}