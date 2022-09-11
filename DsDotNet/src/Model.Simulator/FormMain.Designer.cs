
using Microsoft.Msagl.Drawing;

namespace Model.Simulator
{
    partial class FormMain
    {
        /// <summary>
        /// 필수 디자이너 변수입니다.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 사용 중인 모든 리소스를 정리합니다.
        /// </summary>
        /// <param name="disposing">관리되는 리소스를 삭제해야 하면 true이고, 그렇지 않으면 false입니다.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form 디자이너에서 생성한 코드

        /// <summary>
        /// 디자이너 지원에 필요한 메서드입니다. 
        /// 이 메서드의 내용을 코드 편집기로 수정하지 마세요.
        /// </summary>
        private void InitializeComponent()
        {
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.xtraTabControl_My = new System.Windows.Forms.TabControl();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.button_Stop = new System.Windows.Forms.Button();
            this.button_TestStart = new System.Windows.Forms.Button();
            this.button_Compile = new System.Windows.Forms.Button();
            this.button_TestORG = new System.Windows.Forms.Button();
            this.button_Run = new System.Windows.Forms.Button();
            this.richTextBox_ds = new System.Windows.Forms.RichTextBox();
            this.splitContainer4 = new System.Windows.Forms.SplitContainer();
            this.button_ClearLog = new System.Windows.Forms.Button();
            this.richTextBox_Debug = new System.Windows.Forms.RichTextBox();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.splitter1 = new System.Windows.Forms.Splitter();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer4)).BeginInit();
            this.splitContainer4.Panel1.SuspendLayout();
            this.splitContainer4.Panel2.SuspendLayout();
            this.splitContainer4.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.xtraTabControl_My);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.splitContainer2);
            this.splitContainer1.Size = new System.Drawing.Size(1373, 603);
            this.splitContainer1.SplitterDistance = 829;
            this.splitContainer1.TabIndex = 18;
            // 
            // xtraTabControl_My
            // 
            this.xtraTabControl_My.Dock = System.Windows.Forms.DockStyle.Fill;
            this.xtraTabControl_My.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.xtraTabControl_My.HotTrack = true;
            this.xtraTabControl_My.Location = new System.Drawing.Point(0, 0);
            this.xtraTabControl_My.Name = "xtraTabControl_My";
            this.xtraTabControl_My.SelectedIndex = 0;
            this.xtraTabControl_My.Size = new System.Drawing.Size(829, 603);
            this.xtraTabControl_My.TabIndex = 6;
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitContainer2.Location = new System.Drawing.Point(0, 0);
            this.splitContainer2.Name = "splitContainer2";
            this.splitContainer2.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.BackColor = System.Drawing.SystemColors.Control;
            this.splitContainer2.Panel1.Controls.Add(this.button_Stop);
            this.splitContainer2.Panel1.Controls.Add(this.button_TestStart);
            this.splitContainer2.Panel1.Controls.Add(this.button_Compile);
            this.splitContainer2.Panel1.Controls.Add(this.button_TestORG);
            this.splitContainer2.Panel1.Controls.Add(this.button_Run);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.richTextBox_ds);
            this.splitContainer2.Size = new System.Drawing.Size(540, 603);
            this.splitContainer2.SplitterDistance = 220;
            this.splitContainer2.TabIndex = 4;
            // 
            // button_Stop
            // 
            this.button_Stop.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button_Stop.Location = new System.Drawing.Point(49, 68);
            this.button_Stop.Name = "button_Stop";
            this.button_Stop.Size = new System.Drawing.Size(85, 23);
            this.button_Stop.TabIndex = 5;
            this.button_Stop.Text = "Stop";
            this.button_Stop.UseVisualStyleBackColor = true;
            this.button_Stop.Click += new System.EventHandler(this.button_Stop_Click);
            // 
            // button_TestStart
            // 
            this.button_TestStart.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button_TestStart.Location = new System.Drawing.Point(140, 68);
            this.button_TestStart.Name = "button_TestStart";
            this.button_TestStart.Size = new System.Drawing.Size(85, 23);
            this.button_TestStart.TabIndex = 1;
            this.button_TestStart.Text = "TEST 시작";
            this.button_TestStart.UseVisualStyleBackColor = true;
            this.button_TestStart.Click += new System.EventHandler(this.button_TestStart_Click);
            // 
            // button_Compile
            // 
            this.button_Compile.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button_Compile.Location = new System.Drawing.Point(49, 18);
            this.button_Compile.Name = "button_Compile";
            this.button_Compile.Size = new System.Drawing.Size(85, 23);
            this.button_Compile.TabIndex = 4;
            this.button_Compile.Text = "Compile";
            this.button_Compile.UseVisualStyleBackColor = true;
            this.button_Compile.Click += new System.EventHandler(this.button_Compile_Click);
            // 
            // button_TestORG
            // 
            this.button_TestORG.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button_TestORG.Location = new System.Drawing.Point(140, 42);
            this.button_TestORG.Name = "button_TestORG";
            this.button_TestORG.Size = new System.Drawing.Size(85, 23);
            this.button_TestORG.TabIndex = 1;
            this.button_TestORG.Text = "TEST원위치";
            this.button_TestORG.UseVisualStyleBackColor = true;
            this.button_TestORG.Click += new System.EventHandler(this.button_TestORG_Click);
            // 
            // button_Run
            // 
            this.button_Run.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button_Run.Location = new System.Drawing.Point(49, 42);
            this.button_Run.Name = "button_Run";
            this.button_Run.Size = new System.Drawing.Size(85, 23);
            this.button_Run.TabIndex = 3;
            this.button_Run.Text = "Run";
            this.button_Run.UseVisualStyleBackColor = true;
            this.button_Run.Click += new System.EventHandler(this.button_Run_Click);
            // 
            // richTextBox_ds
            // 
            this.richTextBox_ds.BackColor = System.Drawing.SystemColors.Control;
            this.richTextBox_ds.Dock = System.Windows.Forms.DockStyle.Fill;
            this.richTextBox_ds.Location = new System.Drawing.Point(0, 0);
            this.richTextBox_ds.Name = "richTextBox_ds";
            this.richTextBox_ds.Size = new System.Drawing.Size(540, 379);
            this.richTextBox_ds.TabIndex = 2;
            this.richTextBox_ds.Text = "";
            // 
            // splitContainer4
            // 
            this.splitContainer4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer4.Location = new System.Drawing.Point(0, 0);
            this.splitContainer4.Name = "splitContainer4";
            this.splitContainer4.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer4.Panel1
            // 
            this.splitContainer4.Panel1.Controls.Add(this.splitContainer1);
            // 
            // splitContainer4.Panel2
            // 
            this.splitContainer4.Panel2.Controls.Add(this.button_ClearLog);
            this.splitContainer4.Panel2.Controls.Add(this.richTextBox_Debug);
            this.splitContainer4.Size = new System.Drawing.Size(1373, 746);
            this.splitContainer4.SplitterDistance = 603;
            this.splitContainer4.TabIndex = 20;
            // 
            // button_ClearLog
            // 
            this.button_ClearLog.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button_ClearLog.Location = new System.Drawing.Point(1259, 5);
            this.button_ClearLog.Name = "button_ClearLog";
            this.button_ClearLog.Size = new System.Drawing.Size(85, 23);
            this.button_ClearLog.TabIndex = 1;
            this.button_ClearLog.Text = "Clear Log";
            this.button_ClearLog.UseVisualStyleBackColor = true;
            this.button_ClearLog.Click += new System.EventHandler(this.button_ClearLog_Click);
            // 
            // richTextBox_Debug
            // 
            this.richTextBox_Debug.Dock = System.Windows.Forms.DockStyle.Fill;
            this.richTextBox_Debug.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.richTextBox_Debug.Location = new System.Drawing.Point(0, 0);
            this.richTextBox_Debug.Name = "richTextBox_Debug";
            this.richTextBox_Debug.ReadOnly = true;
            this.richTextBox_Debug.Size = new System.Drawing.Size(1373, 139);
            this.richTextBox_Debug.TabIndex = 0;
            this.richTextBox_Debug.Text = "";
            // 
            // progressBar1
            // 
            this.progressBar1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.progressBar1.Location = new System.Drawing.Point(0, 749);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(1373, 26);
            this.progressBar1.TabIndex = 2;
            // 
            // splitter1
            // 
            this.splitter1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.splitter1.Enabled = false;
            this.splitter1.Location = new System.Drawing.Point(0, 746);
            this.splitter1.Name = "splitter1";
            this.splitter1.Size = new System.Drawing.Size(1373, 3);
            this.splitter1.TabIndex = 22;
            this.splitter1.TabStop = false;
            // 
            // FormMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.ClientSize = new System.Drawing.Size(1373, 775);
            this.Controls.Add(this.splitContainer4);
            this.Controls.Add(this.splitter1);
            this.Controls.Add(this.progressBar1);
            this.ForeColor = System.Drawing.Color.SlateBlue;
            this.Icon = global::Model.Simulator.Properties.Resources.logo_Dualsoft;
            this.IsMdiContainer = true;
            this.Name = "FormMain";
            this.Text = "Dualsoft";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Form1_KeyDown);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
            this.splitContainer2.ResumeLayout(false);
            this.splitContainer4.Panel1.ResumeLayout(false);
            this.splitContainer4.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer4)).EndInit();
            this.splitContainer4.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.SplitContainer splitContainer4;
        private System.Windows.Forms.RichTextBox richTextBox_Debug;
        private System.Windows.Forms.Button button_ClearLog;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.TabControl xtraTabControl_My;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.RichTextBox richTextBox_ds;
        private System.Windows.Forms.Splitter splitter1;
        private System.Windows.Forms.Button button_TestStart;
        private System.Windows.Forms.Button button_TestORG;
        private System.Windows.Forms.Button button_Run;
        private System.Windows.Forms.Button button_Compile;
        private System.Windows.Forms.Button button_Stop;
    }
}

