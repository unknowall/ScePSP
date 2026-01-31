namespace ScePSP.UI
{
    partial class SetForm
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
            gbCPU = new System.Windows.Forms.GroupBox();
            NumDepth = new System.Windows.Forms.NumericUpDown();
            ChkFastMem = new System.Windows.Forms.CheckBox();
            ChkDynTick = new System.Windows.Forms.CheckBox();
            ChkTail = new System.Windows.Forms.CheckBox();
            ChkDynL = new System.Windows.Forms.CheckBox();
            ChkDyn = new System.Windows.Forms.CheckBox();
            gbGE = new System.Windows.Forms.GroupBox();
            LabIRRes = new System.Windows.Forms.Label();
            NumIR = new System.Windows.Forms.NumericUpDown();
            LabIR = new System.Windows.Forms.Label();
            CbScaleMode = new System.Windows.Forms.ComboBox();
            LabTexMode = new System.Windows.Forms.Label();
            NumTexScale = new System.Windows.Forms.NumericUpDown();
            LabTexFilter = new System.Windows.Forms.Label();
            NumTexHit = new System.Windows.Forms.NumericUpDown();
            LabTexHit = new System.Windows.Forms.Label();
            NumTexCache = new System.Windows.Forms.NumericUpDown();
            LabMaxCache = new System.Windows.Forms.Label();
            gbME = new System.Windows.Forms.GroupBox();
            NumAudio = new System.Windows.Forms.NumericUpDown();
            LabAudio = new System.Windows.Forms.Label();
            NumH264 = new System.Windows.Forms.NumericUpDown();
            LabH264 = new System.Windows.Forms.Label();
            NumAT3 = new System.Windows.Forms.NumericUpDown();
            labAt3 = new System.Windows.Forms.Label();
            BtnApply = new System.Windows.Forms.Button();
            BtnCancel = new System.Windows.Forms.Button();
            gbCPU.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)NumDepth).BeginInit();
            gbGE.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)NumIR).BeginInit();
            ((System.ComponentModel.ISupportInitialize)NumTexScale).BeginInit();
            ((System.ComponentModel.ISupportInitialize)NumTexHit).BeginInit();
            ((System.ComponentModel.ISupportInitialize)NumTexCache).BeginInit();
            gbME.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)NumAudio).BeginInit();
            ((System.ComponentModel.ISupportInitialize)NumH264).BeginInit();
            ((System.ComponentModel.ISupportInitialize)NumAT3).BeginInit();
            SuspendLayout();
            // 
            // gbCPU
            // 
            gbCPU.Controls.Add(NumDepth);
            gbCPU.Controls.Add(ChkFastMem);
            gbCPU.Controls.Add(ChkDynTick);
            gbCPU.Controls.Add(ChkTail);
            gbCPU.Controls.Add(ChkDynL);
            gbCPU.Controls.Add(ChkDyn);
            gbCPU.Location = new System.Drawing.Point(12, 12);
            gbCPU.Name = "gbCPU";
            gbCPU.Size = new System.Drawing.Size(418, 154);
            gbCPU.TabIndex = 0;
            gbCPU.TabStop = false;
            gbCPU.Text = "CPU";
            // 
            // NumDepth
            // 
            NumDepth.Location = new System.Drawing.Point(222, 27);
            NumDepth.Maximum = new decimal(new int[] { 5, 0, 0, 0 });
            NumDepth.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            NumDepth.Name = "NumDepth";
            NumDepth.Size = new System.Drawing.Size(79, 23);
            NumDepth.TabIndex = 1;
            NumDepth.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // ChkFastMem
            // 
            ChkFastMem.AutoSize = true;
            ChkFastMem.Location = new System.Drawing.Point(13, 113);
            ChkFastMem.Name = "ChkFastMem";
            ChkFastMem.Size = new System.Drawing.Size(123, 21);
            ChkFastMem.TabIndex = 5;
            ChkFastMem.Text = "使用快速内存访问";
            ChkFastMem.UseVisualStyleBackColor = true;
            // 
            // ChkDynTick
            // 
            ChkDynTick.AutoSize = true;
            ChkDynTick.Location = new System.Drawing.Point(279, 71);
            ChkDynTick.Name = "ChkDynTick";
            ChkDynTick.Size = new System.Drawing.Size(98, 21);
            ChkDynTick.TabIndex = 4;
            ChkDynTick.Text = "使用Tick调用";
            ChkDynTick.UseVisualStyleBackColor = true;
            // 
            // ChkTail
            // 
            ChkTail.AutoSize = true;
            ChkTail.Location = new System.Drawing.Point(140, 71);
            ChkTail.Name = "ChkTail";
            ChkTail.Size = new System.Drawing.Size(111, 21);
            ChkTail.TabIndex = 3;
            ChkTail.Text = "使用尾调用优化";
            ChkTail.UseVisualStyleBackColor = true;
            // 
            // ChkDynL
            // 
            ChkDynL.AutoSize = true;
            ChkDynL.Location = new System.Drawing.Point(13, 71);
            ChkDynL.Name = "ChkDynL";
            ChkDynL.Size = new System.Drawing.Size(99, 21);
            ChkDynL.TabIndex = 2;
            ChkDynL.Text = "使用深度优化";
            ChkDynL.UseVisualStyleBackColor = true;
            // 
            // ChkDyn
            // 
            ChkDyn.AutoSize = true;
            ChkDyn.Location = new System.Drawing.Point(13, 29);
            ChkDyn.Name = "ChkDyn";
            ChkDyn.Size = new System.Drawing.Size(159, 21);
            ChkDyn.TabIndex = 0;
            ChkDyn.Text = "后台动态重编译，深度：";
            ChkDyn.UseVisualStyleBackColor = true;
            // 
            // gbGE
            // 
            gbGE.Controls.Add(LabIRRes);
            gbGE.Controls.Add(NumIR);
            gbGE.Controls.Add(LabIR);
            gbGE.Controls.Add(CbScaleMode);
            gbGE.Controls.Add(LabTexMode);
            gbGE.Controls.Add(NumTexScale);
            gbGE.Controls.Add(LabTexFilter);
            gbGE.Controls.Add(NumTexHit);
            gbGE.Controls.Add(LabTexHit);
            gbGE.Controls.Add(NumTexCache);
            gbGE.Controls.Add(LabMaxCache);
            gbGE.Location = new System.Drawing.Point(12, 181);
            gbGE.Name = "gbGE";
            gbGE.Size = new System.Drawing.Size(418, 147);
            gbGE.TabIndex = 1;
            gbGE.TabStop = false;
            gbGE.Text = "GE - 数字越大内存/显存占用越高";
            // 
            // LabIRRes
            // 
            LabIRRes.AutoSize = true;
            LabIRRes.Location = new System.Drawing.Point(223, 109);
            LabIRRes.Name = "LabIRRes";
            LabIRRes.Size = new System.Drawing.Size(80, 17);
            LabIRRes.TabIndex = 10;
            LabIRRes.Text = "( 480 x 272 )";
            // 
            // NumIR
            // 
            NumIR.Location = new System.Drawing.Point(140, 106);
            NumIR.Maximum = new decimal(new int[] { 12, 0, 0, 0 });
            NumIR.Minimum = new decimal(new int[] { 3, 0, 0, 0 });
            NumIR.Name = "NumIR";
            NumIR.Size = new System.Drawing.Size(60, 23);
            NumIR.TabIndex = 9;
            NumIR.Value = new decimal(new int[] { 3, 0, 0, 0 });
            NumIR.ValueChanged += numericUpDown1_ValueChanged;
            // 
            // LabIR
            // 
            LabIR.AutoSize = true;
            LabIR.Location = new System.Drawing.Point(13, 110);
            LabIR.Name = "LabIR";
            LabIR.Size = new System.Drawing.Size(92, 17);
            LabIR.TabIndex = 8;
            LabIR.Text = "内部分辨率倍率";
            // 
            // CbScaleMode
            // 
            CbScaleMode.FormattingEnabled = true;
            CbScaleMode.Items.AddRange(new object[] { "None", "Neighbor", "Jinc", "xBR" });
            CbScaleMode.Location = new System.Drawing.Point(333, 64);
            CbScaleMode.Name = "CbScaleMode";
            CbScaleMode.Size = new System.Drawing.Size(79, 25);
            CbScaleMode.TabIndex = 7;
            // 
            // LabTexMode
            // 
            LabTexMode.AutoSize = true;
            LabTexMode.Location = new System.Drawing.Point(223, 67);
            LabTexMode.Name = "LabTexMode";
            LabTexMode.Size = new System.Drawing.Size(80, 17);
            LabTexMode.TabIndex = 6;
            LabTexMode.Text = "材质增强方式";
            // 
            // NumTexScale
            // 
            NumTexScale.Increment = new decimal(new int[] { 2, 0, 0, 0 });
            NumTexScale.Location = new System.Drawing.Point(140, 65);
            NumTexScale.Maximum = new decimal(new int[] { 10, 0, 0, 0 });
            NumTexScale.Minimum = new decimal(new int[] { 2, 0, 0, 0 });
            NumTexScale.Name = "NumTexScale";
            NumTexScale.Size = new System.Drawing.Size(60, 23);
            NumTexScale.TabIndex = 5;
            NumTexScale.Value = new decimal(new int[] { 2, 0, 0, 0 });
            // 
            // LabTexFilter
            // 
            LabTexFilter.AutoSize = true;
            LabTexFilter.Location = new System.Drawing.Point(13, 69);
            LabTexFilter.Name = "LabTexFilter";
            LabTexFilter.Size = new System.Drawing.Size(56, 17);
            LabTexFilter.TabIndex = 4;
            LabTexFilter.Text = "材质增强";
            // 
            // NumTexHit
            // 
            NumTexHit.Location = new System.Drawing.Point(333, 26);
            NumTexHit.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            NumTexHit.Minimum = new decimal(new int[] { 50, 0, 0, 0 });
            NumTexHit.Name = "NumTexHit";
            NumTexHit.Size = new System.Drawing.Size(79, 23);
            NumTexHit.TabIndex = 3;
            NumTexHit.Value = new decimal(new int[] { 50, 0, 0, 0 });
            // 
            // LabTexHit
            // 
            LabTexHit.AutoSize = true;
            LabTexHit.Location = new System.Drawing.Point(223, 28);
            LabTexHit.Name = "LabTexHit";
            LabTexHit.Size = new System.Drawing.Size(104, 17);
            LabTexHit.TabIndex = 2;
            LabTexHit.Text = "材质最低命中阈值";
            // 
            // NumTexCache
            // 
            NumTexCache.Location = new System.Drawing.Point(140, 26);
            NumTexCache.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            NumTexCache.Minimum = new decimal(new int[] { 100, 0, 0, 0 });
            NumTexCache.Name = "NumTexCache";
            NumTexCache.Size = new System.Drawing.Size(60, 23);
            NumTexCache.TabIndex = 1;
            NumTexCache.Value = new decimal(new int[] { 200, 0, 0, 0 });
            // 
            // LabMaxCache
            // 
            LabMaxCache.AutoSize = true;
            LabMaxCache.Location = new System.Drawing.Point(13, 28);
            LabMaxCache.Name = "LabMaxCache";
            LabMaxCache.Size = new System.Drawing.Size(80, 17);
            LabMaxCache.TabIndex = 0;
            LabMaxCache.Text = "最大材质缓存";
            // 
            // gbME
            // 
            gbME.Controls.Add(NumAudio);
            gbME.Controls.Add(LabAudio);
            gbME.Controls.Add(NumH264);
            gbME.Controls.Add(LabH264);
            gbME.Controls.Add(NumAT3);
            gbME.Controls.Add(labAt3);
            gbME.Location = new System.Drawing.Point(12, 344);
            gbME.Name = "gbME";
            gbME.Size = new System.Drawing.Size(418, 111);
            gbME.TabIndex = 2;
            gbME.TabStop = false;
            gbME.Text = "ME - 缓冲占用主机内存";
            // 
            // NumAudio
            // 
            NumAudio.Location = new System.Drawing.Point(140, 68);
            NumAudio.Minimum = new decimal(new int[] { 16, 0, 0, 0 });
            NumAudio.Name = "NumAudio";
            NumAudio.Size = new System.Drawing.Size(60, 23);
            NumAudio.TabIndex = 5;
            NumAudio.Value = new decimal(new int[] { 16, 0, 0, 0 });
            // 
            // LabAudio
            // 
            LabAudio.AutoSize = true;
            LabAudio.Location = new System.Drawing.Point(13, 72);
            LabAudio.Name = "LabAudio";
            LabAudio.Size = new System.Drawing.Size(107, 17);
            LabAudio.TabIndex = 4;
            LabAudio.Text = "音频回放缓冲(MS)";
            // 
            // NumH264
            // 
            NumH264.Location = new System.Drawing.Point(333, 26);
            NumH264.Maximum = new decimal(new int[] { 8192, 0, 0, 0 });
            NumH264.Minimum = new decimal(new int[] { 512, 0, 0, 0 });
            NumH264.Name = "NumH264";
            NumH264.Size = new System.Drawing.Size(79, 23);
            NumH264.TabIndex = 3;
            NumH264.Value = new decimal(new int[] { 512, 0, 0, 0 });
            // 
            // LabH264
            // 
            LabH264.AutoSize = true;
            LabH264.Location = new System.Drawing.Point(222, 30);
            LabH264.Name = "LabH264";
            LabH264.Size = new System.Drawing.Size(104, 17);
            LabH264.TabIndex = 2;
            LabH264.Text = "视频解码缓冲(KB)";
            // 
            // NumAT3
            // 
            NumAT3.Location = new System.Drawing.Point(140, 26);
            NumAT3.Maximum = new decimal(new int[] { 1024, 0, 0, 0 });
            NumAT3.Minimum = new decimal(new int[] { 64, 0, 0, 0 });
            NumAT3.Name = "NumAT3";
            NumAT3.Size = new System.Drawing.Size(60, 23);
            NumAT3.TabIndex = 1;
            NumAT3.Value = new decimal(new int[] { 64, 0, 0, 0 });
            // 
            // labAt3
            // 
            labAt3.AutoSize = true;
            labAt3.Location = new System.Drawing.Point(13, 30);
            labAt3.Name = "labAt3";
            labAt3.Size = new System.Drawing.Size(104, 17);
            labAt3.TabIndex = 0;
            labAt3.Text = "音频解码缓冲(KB)";
            // 
            // BtnApply
            // 
            BtnApply.Location = new System.Drawing.Point(264, 467);
            BtnApply.Name = "BtnApply";
            BtnApply.Size = new System.Drawing.Size(75, 30);
            BtnApply.TabIndex = 3;
            BtnApply.Text = "应用";
            BtnApply.UseVisualStyleBackColor = true;
            BtnApply.Click += BtnApply_Click;
            // 
            // BtnCancel
            // 
            BtnCancel.Location = new System.Drawing.Point(355, 467);
            BtnCancel.Name = "BtnCancel";
            BtnCancel.Size = new System.Drawing.Size(75, 30);
            BtnCancel.TabIndex = 4;
            BtnCancel.Text = "取消";
            BtnCancel.UseVisualStyleBackColor = true;
            BtnCancel.Click += BtnCancel_Click;
            // 
            // SetForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(442, 505);
            Controls.Add(BtnCancel);
            Controls.Add(BtnApply);
            Controls.Add(gbME);
            Controls.Add(gbGE);
            Controls.Add(gbCPU);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Name = "SetForm";
            Text = "Setting";
            Shown += SetForm_Shown;
            gbCPU.ResumeLayout(false);
            gbCPU.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)NumDepth).EndInit();
            gbGE.ResumeLayout(false);
            gbGE.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)NumIR).EndInit();
            ((System.ComponentModel.ISupportInitialize)NumTexScale).EndInit();
            ((System.ComponentModel.ISupportInitialize)NumTexHit).EndInit();
            ((System.ComponentModel.ISupportInitialize)NumTexCache).EndInit();
            gbME.ResumeLayout(false);
            gbME.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)NumAudio).EndInit();
            ((System.ComponentModel.ISupportInitialize)NumH264).EndInit();
            ((System.ComponentModel.ISupportInitialize)NumAT3).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.GroupBox gbCPU;
        private System.Windows.Forms.CheckBox ChkDynL;
        private System.Windows.Forms.CheckBox ChkDyn;
        private System.Windows.Forms.CheckBox ChkDynTick;
        private System.Windows.Forms.CheckBox ChkTail;
        private System.Windows.Forms.CheckBox ChkFastMem;
        private System.Windows.Forms.NumericUpDown NumDepth;
        private System.Windows.Forms.GroupBox gbGE;
        private System.Windows.Forms.NumericUpDown NumTexCache;
        private System.Windows.Forms.Label LabMaxCache;
        private System.Windows.Forms.Label LabTexFilter;
        private System.Windows.Forms.NumericUpDown NumTexHit;
        private System.Windows.Forms.Label LabTexHit;
        private System.Windows.Forms.ComboBox CbScaleMode;
        private System.Windows.Forms.Label LabTexMode;
        private System.Windows.Forms.NumericUpDown NumTexScale;
        private System.Windows.Forms.Label LabIRRes;
        private System.Windows.Forms.NumericUpDown NumIR;
        private System.Windows.Forms.Label LabIR;
        private System.Windows.Forms.GroupBox gbME;
        private System.Windows.Forms.NumericUpDown NumAT3;
        private System.Windows.Forms.Label labAt3;
        private System.Windows.Forms.Button BtnApply;
        private System.Windows.Forms.Button BtnCancel;
        private System.Windows.Forms.Label LabAudio;
        private System.Windows.Forms.NumericUpDown NumH264;
        private System.Windows.Forms.Label LabH264;
        private System.Windows.Forms.NumericUpDown NumAudio;
    }
}