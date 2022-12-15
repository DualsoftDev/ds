
namespace Dual.Model.Import
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
            this.splitter2 = new System.Windows.Forms.Splitter();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.button_OpenFolder = new System.Windows.Forms.Button();
            this.button_CreateExcel = new System.Windows.Forms.Button();
            this.pictureBox_ppt = new System.Windows.Forms.PictureBox();
            this.pictureBox_xls = new System.Windows.Forms.PictureBox();
            this.splitContainer5 = new System.Windows.Forms.SplitContainer();
            this.richTextBox_ds = new System.Windows.Forms.RichTextBox();
            this.xtraTabControl_Ex = new System.Windows.Forms.TabControl();
            this.button_copy = new System.Windows.Forms.Button();
            this.button_TestStart = new System.Windows.Forms.Button();
            this.button_TestORG = new System.Windows.Forms.Button();
            this.splitContainer4 = new System.Windows.Forms.SplitContainer();
            this.button_Stop = new System.Windows.Forms.Button();
            this.button_Reset = new System.Windows.Forms.Button();
            this.button_start = new System.Windows.Forms.Button();
            this.comboBox_Segment = new System.Windows.Forms.ComboBox();
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
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox_ppt)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox_xls)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer5)).BeginInit();
            this.splitContainer5.Panel1.SuspendLayout();
            this.splitContainer5.Panel2.SuspendLayout();
            this.splitContainer5.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer4)).BeginInit();
            this.splitContainer4.Panel1.SuspendLayout();
            this.splitContainer4.Panel2.SuspendLayout();
            this.splitContainer4.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.xtraTabControl_My);
            this.splitContainer1.Panel1.Controls.Add(this.splitter2);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.splitContainer2);
            this.splitContainer1.Size = new System.Drawing.Size(1373, 346);
            this.splitContainer1.SplitterDistance = 672;
            this.splitContainer1.TabIndex = 18;
            // 
            // xtraTabControl_My
            // 
            this.xtraTabControl_My.Dock = System.Windows.Forms.DockStyle.Fill;
            this.xtraTabControl_My.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.xtraTabControl_My.HotTrack = true;
            this.xtraTabControl_My.Location = new System.Drawing.Point(3, 0);
            this.xtraTabControl_My.Name = "xtraTabControl_My";
            this.xtraTabControl_My.SelectedIndex = 0;
            this.xtraTabControl_My.Size = new System.Drawing.Size(669, 346);
            this.xtraTabControl_My.TabIndex = 8;
            // 
            // splitter2
            // 
            this.splitter2.Location = new System.Drawing.Point(0, 0);
            this.splitter2.Name = "splitter2";
            this.splitter2.Size = new System.Drawing.Size(3, 346);
            this.splitter2.TabIndex = 7;
            this.splitter2.TabStop = false;
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
            this.splitContainer2.Panel1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(33)))), ((int)(((byte)(33)))));
            this.splitContainer2.Panel1.Controls.Add(this.button_OpenFolder);
            this.splitContainer2.Panel1.Controls.Add(this.button_CreateExcel);
            this.splitContainer2.Panel1.Controls.Add(this.pictureBox_ppt);
            this.splitContainer2.Panel1.Controls.Add(this.pictureBox_xls);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.splitContainer5);
            this.splitContainer2.Panel2.Controls.Add(this.button_copy);
            this.splitContainer2.Size = new System.Drawing.Size(697, 346);
            this.splitContainer2.SplitterDistance = 220;
            this.splitContainer2.TabIndex = 4;
            // 
            // button_OpenFolder
            // 
            this.button_OpenFolder.BackColor = System.Drawing.Color.DarkSeaGreen;
            this.button_OpenFolder.Location = new System.Drawing.Point(304, 165);
            this.button_OpenFolder.Name = "button_OpenFolder";
            this.button_OpenFolder.Size = new System.Drawing.Size(72, 33);
            this.button_OpenFolder.TabIndex = 4;
            this.button_OpenFolder.Text = "폴더열기";
            this.button_OpenFolder.UseVisualStyleBackColor = false;
            this.button_OpenFolder.Visible = false;
            this.button_OpenFolder.Click += new System.EventHandler(this.button_OpenFolder_Click);
            // 
            // button_CreateExcel
            // 
            this.button_CreateExcel.BackColor = System.Drawing.Color.DarkSeaGreen;
            this.button_CreateExcel.Location = new System.Drawing.Point(82, 165);
            this.button_CreateExcel.Name = "button_CreateExcel";
            this.button_CreateExcel.Size = new System.Drawing.Size(72, 33);
            this.button_CreateExcel.TabIndex = 3;
            this.button_CreateExcel.Text = "엑셀생성";
            this.button_CreateExcel.UseVisualStyleBackColor = false;
            this.button_CreateExcel.Visible = false;
            this.button_CreateExcel.Click += new System.EventHandler(this.button_CreateExcel_Click);
            // 
            // pictureBox_ppt
            // 
            this.pictureBox_ppt.Image = global::Model.Import.Viewer.Properties.Resources.IMPORT_PPT;
            this.pictureBox_ppt.Location = new System.Drawing.Point(26, 10);
            this.pictureBox_ppt.Name = "pictureBox_ppt";
            this.pictureBox_ppt.Size = new System.Drawing.Size(418, 97);
            this.pictureBox_ppt.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox_ppt.TabIndex = 0;
            this.pictureBox_ppt.TabStop = false;
            // 
            // pictureBox_xls
            // 
            this.pictureBox_xls.Image = global::Model.Import.Viewer.Properties.Resources.IMPORT_XLS;
            this.pictureBox_xls.Location = new System.Drawing.Point(26, 113);
            this.pictureBox_xls.Name = "pictureBox_xls";
            this.pictureBox_xls.Size = new System.Drawing.Size(418, 98);
            this.pictureBox_xls.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox_xls.TabIndex = 1;
            this.pictureBox_xls.TabStop = false;
            this.pictureBox_xls.Visible = false;
            // 
            // splitContainer5
            // 
            this.splitContainer5.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer5.Location = new System.Drawing.Point(0, 0);
            this.splitContainer5.Name = "splitContainer5";
            this.splitContainer5.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer5.Panel1
            // 
            this.splitContainer5.Panel1.Controls.Add(this.richTextBox_ds);
            // 
            // splitContainer5.Panel2
            // 
            this.splitContainer5.Panel2.Controls.Add(this.xtraTabControl_Ex);
            this.splitContainer5.Panel2Collapsed = true;
            this.splitContainer5.Size = new System.Drawing.Size(697, 122);
            this.splitContainer5.SplitterDistance = 97;
            this.splitContainer5.TabIndex = 3;
            // 
            // richTextBox_ds
            // 
            this.richTextBox_ds.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(33)))), ((int)(((byte)(33)))));
            this.richTextBox_ds.Dock = System.Windows.Forms.DockStyle.Fill;
            this.richTextBox_ds.Location = new System.Drawing.Point(0, 0);
            this.richTextBox_ds.Name = "richTextBox_ds";
            this.richTextBox_ds.ReadOnly = true;
            this.richTextBox_ds.Size = new System.Drawing.Size(697, 122);
            this.richTextBox_ds.TabIndex = 2;
            this.richTextBox_ds.Text = "";
            // 
            // xtraTabControl_Ex
            // 
            this.xtraTabControl_Ex.Alignment = System.Windows.Forms.TabAlignment.Bottom;
            this.xtraTabControl_Ex.Dock = System.Windows.Forms.DockStyle.Fill;
            this.xtraTabControl_Ex.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.xtraTabControl_Ex.HotTrack = true;
            this.xtraTabControl_Ex.Location = new System.Drawing.Point(0, 0);
            this.xtraTabControl_Ex.Name = "xtraTabControl_Ex";
            this.xtraTabControl_Ex.SelectedIndex = 0;
            this.xtraTabControl_Ex.Size = new System.Drawing.Size(150, 46);
            this.xtraTabControl_Ex.TabIndex = 6;
            // 
            // button_copy
            // 
            this.button_copy.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button_copy.Location = new System.Drawing.Point(583, 13);
            this.button_copy.Name = "button_copy";
            this.button_copy.Size = new System.Drawing.Size(85, 34);
            this.button_copy.TabIndex = 1;
            this.button_copy.Text = "클립보드 모델복사";
            this.button_copy.UseVisualStyleBackColor = true;
            this.button_copy.Click += new System.EventHandler(this.button_copy_Click);
            // 
            // button_TestStart
            // 
            this.button_TestStart.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button_TestStart.Location = new System.Drawing.Point(1259, 55);
            this.button_TestStart.Name = "button_TestStart";
            this.button_TestStart.Size = new System.Drawing.Size(85, 23);
            this.button_TestStart.TabIndex = 1;
            this.button_TestStart.Text = "TEST 시작";
            this.button_TestStart.UseVisualStyleBackColor = true;
            this.button_TestStart.Visible = false;
            this.button_TestStart.Click += new System.EventHandler(this.button_TestStart_Click);
            // 
            // button_TestORG
            // 
            this.button_TestORG.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button_TestORG.Location = new System.Drawing.Point(1259, 29);
            this.button_TestORG.Name = "button_TestORG";
            this.button_TestORG.Size = new System.Drawing.Size(85, 23);
            this.button_TestORG.TabIndex = 1;
            this.button_TestORG.Text = "TEST원위치";
            this.button_TestORG.UseVisualStyleBackColor = true;
            this.button_TestORG.Visible = false;
            this.button_TestORG.Click += new System.EventHandler(this.button_TestORG_Click);
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
            this.splitContainer4.Panel2.Controls.Add(this.button_Stop);
            this.splitContainer4.Panel2.Controls.Add(this.button_Reset);
            this.splitContainer4.Panel2.Controls.Add(this.button_start);
            this.splitContainer4.Panel2.Controls.Add(this.comboBox_Segment);
            this.splitContainer4.Panel2.Controls.Add(this.button_TestStart);
            this.splitContainer4.Panel2.Controls.Add(this.button_TestORG);
            this.splitContainer4.Panel2.Controls.Add(this.button_ClearLog);
            this.splitContainer4.Panel2.Controls.Add(this.richTextBox_Debug);
            this.splitContainer4.Size = new System.Drawing.Size(1373, 746);
            this.splitContainer4.SplitterDistance = 346;
            this.splitContainer4.TabIndex = 20;
            // 
            // button_Stop
            // 
            this.button_Stop.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button_Stop.Location = new System.Drawing.Point(1259, 84);
            this.button_Stop.Name = "button_Stop";
            this.button_Stop.Size = new System.Drawing.Size(85, 23);
            this.button_Stop.TabIndex = 9;
            this.button_Stop.Text = "TEST 멈춤";
            this.button_Stop.UseVisualStyleBackColor = true;
            this.button_Stop.Visible = false;
            this.button_Stop.Click += new System.EventHandler(this.button_Stop_Click);
            // 
            // button_Reset
            // 
            this.button_Reset.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button_Reset.Location = new System.Drawing.Point(1259, 172);
            this.button_Reset.Name = "button_Reset";
            this.button_Reset.Size = new System.Drawing.Size(85, 23);
            this.button_Reset.TabIndex = 8;
            this.button_Reset.Text = "Reset";
            this.button_Reset.UseVisualStyleBackColor = true;
            this.button_Reset.Click += new System.EventHandler(this.button_reset_Click);
            // 
            // button_start
            // 
            this.button_start.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button_start.Location = new System.Drawing.Point(1171, 172);
            this.button_start.Name = "button_start";
            this.button_start.Size = new System.Drawing.Size(85, 23);
            this.button_start.TabIndex = 7;
            this.button_start.Text = "Start";
            this.button_start.UseVisualStyleBackColor = true;
            this.button_start.Click += new System.EventHandler(this.button_start_Click);
            // 
            // comboBox_Segment
            // 
            this.comboBox_Segment.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBox_Segment.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox_Segment.FormattingEnabled = true;
            this.comboBox_Segment.Location = new System.Drawing.Point(1171, 146);
            this.comboBox_Segment.Name = "comboBox_Segment";
            this.comboBox_Segment.Size = new System.Drawing.Size(173, 20);
            this.comboBox_Segment.TabIndex = 6;
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
            this.richTextBox_Debug.Size = new System.Drawing.Size(1373, 396);
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
            this.Icon = global::Model.Import.Viewer.Properties.Resources.logo_Dualsoft;
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
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox_ppt)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox_xls)).EndInit();
            this.splitContainer5.Panel1.ResumeLayout(false);
            this.splitContainer5.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer5)).EndInit();
            this.splitContainer5.ResumeLayout(false);
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
        private System.Windows.Forms.PictureBox pictureBox_ppt;
        private System.Windows.Forms.PictureBox pictureBox_xls;
        private System.Windows.Forms.Button button_ClearLog;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.TabControl xtraTabControl_Ex;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.RichTextBox richTextBox_ds;
        private System.Windows.Forms.Button button_copy;
        private System.Windows.Forms.Splitter splitter1;
        private System.Windows.Forms.Button button_OpenFolder;
        private System.Windows.Forms.Button button_CreateExcel;
        private System.Windows.Forms.Button button_TestStart;
        private System.Windows.Forms.Button button_TestORG;
        private System.Windows.Forms.TabControl xtraTabControl_My;
        private System.Windows.Forms.Splitter splitter2;
        private System.Windows.Forms.SplitContainer splitContainer5;
        private System.Windows.Forms.Button button_Reset;
        private System.Windows.Forms.Button button_start;
        private System.Windows.Forms.ComboBox comboBox_Segment;
        private System.Windows.Forms.Button button_Stop;
    }
}

