
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
            this.splitContainer3 = new System.Windows.Forms.SplitContainer();
            this.xtraTabControl_My = new System.Windows.Forms.TabControl();
            this.xtraTabControl_Ex = new System.Windows.Forms.TabControl();
            this.splitter2 = new System.Windows.Forms.Splitter();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.button_CreatePLC = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.comboBox_Package = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.comboBox_Device = new System.Windows.Forms.ComboBox();
            this.comboBox_System = new System.Windows.Forms.ComboBox();
            this.button_OpenFolder = new System.Windows.Forms.Button();
            this.button_CreateExcel = new System.Windows.Forms.Button();
            this.pictureBox_ppt = new System.Windows.Forms.PictureBox();
            this.pictureBox_xls = new System.Windows.Forms.PictureBox();
            this.richTextBox_ds = new System.Windows.Forms.RichTextBox();
            this.button_copy = new System.Windows.Forms.Button();
            this.button_TestStart = new System.Windows.Forms.Button();
            this.button_TestORG = new System.Windows.Forms.Button();
            this.splitContainer4 = new System.Windows.Forms.SplitContainer();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage_system = new System.Windows.Forms.TabPage();
            this.checkedListBox_sysHMI = new System.Windows.Forms.CheckedListBox();
            this.tabPage_device = new System.Windows.Forms.TabPage();
            this.checkedListBox_Ex = new System.Windows.Forms.CheckedListBox();
            this.tabPage_active = new System.Windows.Forms.TabPage();
            this.checkedListBox_My = new System.Windows.Forms.CheckedListBox();
            this.tabPage_activeFliter = new System.Windows.Forms.TabPage();
            this.listBox_find = new System.Windows.Forms.ListBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.label4 = new System.Windows.Forms.Label();
            this.textBox_activeFind = new System.Windows.Forms.TextBox();
            this.button_ClearLog = new System.Windows.Forms.Button();
            this.comboBox_TestExpr = new System.Windows.Forms.ComboBox();
            this.button_Stop = new System.Windows.Forms.Button();
            this.comboBox_Segment = new System.Windows.Forms.ComboBox();
            this.button_Reset = new System.Windows.Forms.Button();
            this.button_Start = new System.Windows.Forms.Button();
            this.richTextBox_Debug = new System.Windows.Forms.RichTextBox();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.splitter1 = new System.Windows.Forms.Splitter();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer3)).BeginInit();
            this.splitContainer3.Panel1.SuspendLayout();
            this.splitContainer3.Panel2.SuspendLayout();
            this.splitContainer3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox_ppt)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox_xls)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer4)).BeginInit();
            this.splitContainer4.Panel1.SuspendLayout();
            this.splitContainer4.Panel2.SuspendLayout();
            this.splitContainer4.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabPage_system.SuspendLayout();
            this.tabPage_device.SuspendLayout();
            this.tabPage_active.SuspendLayout();
            this.tabPage_activeFliter.SuspendLayout();
            this.panel1.SuspendLayout();
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
            this.splitContainer1.Panel1.Controls.Add(this.splitContainer3);
            this.splitContainer1.Panel1.Controls.Add(this.splitter2);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.splitContainer2);
            this.splitContainer1.Size = new System.Drawing.Size(1262, 361);
            this.splitContainer1.SplitterDistance = 791;
            this.splitContainer1.TabIndex = 18;
            // 
            // splitContainer3
            // 
            this.splitContainer3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer3.Location = new System.Drawing.Point(3, 0);
            this.splitContainer3.Name = "splitContainer3";
            // 
            // splitContainer3.Panel1
            // 
            this.splitContainer3.Panel1.Controls.Add(this.xtraTabControl_My);
            // 
            // splitContainer3.Panel2
            // 
            this.splitContainer3.Panel2.Controls.Add(this.xtraTabControl_Ex);
            this.splitContainer3.Size = new System.Drawing.Size(788, 361);
            this.splitContainer3.SplitterDistance = 565;
            this.splitContainer3.TabIndex = 9;
            // 
            // xtraTabControl_My
            // 
            this.xtraTabControl_My.Dock = System.Windows.Forms.DockStyle.Fill;
            this.xtraTabControl_My.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.xtraTabControl_My.HotTrack = true;
            this.xtraTabControl_My.Location = new System.Drawing.Point(0, 0);
            this.xtraTabControl_My.Name = "xtraTabControl_My";
            this.xtraTabControl_My.SelectedIndex = 0;
            this.xtraTabControl_My.Size = new System.Drawing.Size(565, 361);
            this.xtraTabControl_My.TabIndex = 8;
            // 
            // xtraTabControl_Ex
            // 
            this.xtraTabControl_Ex.Dock = System.Windows.Forms.DockStyle.Fill;
            this.xtraTabControl_Ex.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.xtraTabControl_Ex.HotTrack = true;
            this.xtraTabControl_Ex.Location = new System.Drawing.Point(0, 0);
            this.xtraTabControl_Ex.Name = "xtraTabControl_Ex";
            this.xtraTabControl_Ex.SelectedIndex = 0;
            this.xtraTabControl_Ex.Size = new System.Drawing.Size(219, 361);
            this.xtraTabControl_Ex.TabIndex = 9;
            // 
            // splitter2
            // 
            this.splitter2.Location = new System.Drawing.Point(0, 0);
            this.splitter2.Name = "splitter2";
            this.splitter2.Size = new System.Drawing.Size(3, 361);
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
            this.splitContainer2.Panel1.Controls.Add(this.button_CreatePLC);
            this.splitContainer2.Panel1.Controls.Add(this.label3);
            this.splitContainer2.Panel1.Controls.Add(this.comboBox_Package);
            this.splitContainer2.Panel1.Controls.Add(this.label2);
            this.splitContainer2.Panel1.Controls.Add(this.label1);
            this.splitContainer2.Panel1.Controls.Add(this.comboBox_Device);
            this.splitContainer2.Panel1.Controls.Add(this.comboBox_System);
            this.splitContainer2.Panel1.Controls.Add(this.button_OpenFolder);
            this.splitContainer2.Panel1.Controls.Add(this.button_CreateExcel);
            this.splitContainer2.Panel1.Controls.Add(this.pictureBox_ppt);
            this.splitContainer2.Panel1.Controls.Add(this.pictureBox_xls);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.richTextBox_ds);
            this.splitContainer2.Panel2.Controls.Add(this.button_copy);
            this.splitContainer2.Size = new System.Drawing.Size(467, 361);
            this.splitContainer2.SplitterDistance = 253;
            this.splitContainer2.TabIndex = 4;
            // 
            // button_CreatePLC
            // 
            this.button_CreatePLC.BackColor = System.Drawing.Color.DarkSeaGreen;
            this.button_CreatePLC.Location = new System.Drawing.Point(111, 192);
            this.button_CreatePLC.Name = "button_CreatePLC";
            this.button_CreatePLC.Size = new System.Drawing.Size(48, 33);
            this.button_CreatePLC.TabIndex = 17;
            this.button_CreatePLC.Text = "PLC생성";
            this.button_CreatePLC.UseVisualStyleBackColor = false;
            this.button_CreatePLC.Visible = false;
            this.button_CreatePLC.Click += new System.EventHandler(this.button_CreatePLC_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(24, 116);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(33, 12);
            this.label3.TabIndex = 16;
            this.label3.Text = "배포:";
            // 
            // comboBox_Package
            // 
            this.comboBox_Package.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox_Package.FormattingEnabled = true;
            this.comboBox_Package.Location = new System.Drawing.Point(63, 112);
            this.comboBox_Package.Name = "comboBox_Package";
            this.comboBox_Package.Size = new System.Drawing.Size(91, 20);
            this.comboBox_Package.TabIndex = 15;
            this.comboBox_Package.SelectedIndexChanged += new System.EventHandler(this.comboBox_Package_SelectedIndexChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(311, 117);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(57, 12);
            this.label2.TabIndex = 12;
            this.label2.Text = "디바이스:";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(176, 117);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(45, 12);
            this.label1.TabIndex = 11;
            this.label1.Text = "시스템:";
            // 
            // comboBox_Device
            // 
            this.comboBox_Device.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox_Device.FormattingEnabled = true;
            this.comboBox_Device.Location = new System.Drawing.Point(374, 113);
            this.comboBox_Device.Name = "comboBox_Device";
            this.comboBox_Device.Size = new System.Drawing.Size(67, 20);
            this.comboBox_Device.TabIndex = 10;
            this.comboBox_Device.SelectedIndexChanged += new System.EventHandler(this.comboBox_Device_SelectedIndexChanged);
            // 
            // comboBox_System
            // 
            this.comboBox_System.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox_System.FormattingEnabled = true;
            this.comboBox_System.Location = new System.Drawing.Point(230, 113);
            this.comboBox_System.Name = "comboBox_System";
            this.comboBox_System.Size = new System.Drawing.Size(59, 20);
            this.comboBox_System.TabIndex = 10;
            this.comboBox_System.SelectedIndexChanged += new System.EventHandler(this.comboBox_System_SelectedIndexChanged);
            // 
            // button_OpenFolder
            // 
            this.button_OpenFolder.BackColor = System.Drawing.Color.DarkSeaGreen;
            this.button_OpenFolder.Location = new System.Drawing.Point(304, 194);
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
            this.button_CreateExcel.Location = new System.Drawing.Point(59, 192);
            this.button_CreateExcel.Name = "button_CreateExcel";
            this.button_CreateExcel.Size = new System.Drawing.Size(47, 33);
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
            this.pictureBox_xls.Location = new System.Drawing.Point(26, 140);
            this.pictureBox_xls.Name = "pictureBox_xls";
            this.pictureBox_xls.Size = new System.Drawing.Size(418, 98);
            this.pictureBox_xls.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox_xls.TabIndex = 1;
            this.pictureBox_xls.TabStop = false;
            this.pictureBox_xls.Visible = false;
            // 
            // richTextBox_ds
            // 
            this.richTextBox_ds.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(33)))), ((int)(((byte)(33)))));
            this.richTextBox_ds.Dock = System.Windows.Forms.DockStyle.Fill;
            this.richTextBox_ds.Location = new System.Drawing.Point(0, 0);
            this.richTextBox_ds.Name = "richTextBox_ds";
            this.richTextBox_ds.ReadOnly = true;
            this.richTextBox_ds.Size = new System.Drawing.Size(467, 104);
            this.richTextBox_ds.TabIndex = 2;
            this.richTextBox_ds.Text = "";
            // 
            // button_copy
            // 
            this.button_copy.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button_copy.Location = new System.Drawing.Point(408, 13);
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
            this.button_TestStart.Location = new System.Drawing.Point(16, 79);
            this.button_TestStart.Name = "button_TestStart";
            this.button_TestStart.Size = new System.Drawing.Size(85, 20);
            this.button_TestStart.TabIndex = 1;
            this.button_TestStart.Text = "TEST 시작";
            this.button_TestStart.UseVisualStyleBackColor = true;
            this.button_TestStart.Click += new System.EventHandler(this.button_TestStart_Click);
            // 
            // button_TestORG
            // 
            this.button_TestORG.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button_TestORG.Location = new System.Drawing.Point(16, 51);
            this.button_TestORG.Name = "button_TestORG";
            this.button_TestORG.Size = new System.Drawing.Size(85, 20);
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
            this.splitContainer4.Panel2.Controls.Add(this.tabControl1);
            this.splitContainer4.Panel2.Controls.Add(this.panel1);
            this.splitContainer4.Panel2.Controls.Add(this.richTextBox_Debug);
            this.splitContainer4.Size = new System.Drawing.Size(1262, 781);
            this.splitContainer4.SplitterDistance = 361;
            this.splitContainer4.TabIndex = 20;
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage_system);
            this.tabControl1.Controls.Add(this.tabPage_device);
            this.tabControl1.Controls.Add(this.tabPage_active);
            this.tabControl1.Controls.Add(this.tabPage_activeFliter);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Right;
            this.tabControl1.Location = new System.Drawing.Point(788, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(358, 416);
            this.tabControl1.TabIndex = 21;
            // 
            // tabPage_system
            // 
            this.tabPage_system.Controls.Add(this.checkedListBox_sysHMI);
            this.tabPage_system.Location = new System.Drawing.Point(4, 22);
            this.tabPage_system.Name = "tabPage_system";
            this.tabPage_system.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage_system.Size = new System.Drawing.Size(350, 390);
            this.tabPage_system.TabIndex = 0;
            this.tabPage_system.Text = "system";
            this.tabPage_system.UseVisualStyleBackColor = true;
            // 
            // checkedListBox_sysHMI
            // 
            this.checkedListBox_sysHMI.Dock = System.Windows.Forms.DockStyle.Fill;
            this.checkedListBox_sysHMI.FormattingEnabled = true;
            this.checkedListBox_sysHMI.Items.AddRange(new object[] {
            "Select System"});
            this.checkedListBox_sysHMI.Location = new System.Drawing.Point(3, 3);
            this.checkedListBox_sysHMI.Name = "checkedListBox_sysHMI";
            this.checkedListBox_sysHMI.Size = new System.Drawing.Size(344, 384);
            this.checkedListBox_sysHMI.TabIndex = 20;
            this.checkedListBox_sysHMI.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.checkedListBox_sysHMI_ItemCheck);
            // 
            // tabPage_device
            // 
            this.tabPage_device.Controls.Add(this.checkedListBox_Ex);
            this.tabPage_device.Location = new System.Drawing.Point(4, 22);
            this.tabPage_device.Name = "tabPage_device";
            this.tabPage_device.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage_device.Size = new System.Drawing.Size(350, 390);
            this.tabPage_device.TabIndex = 1;
            this.tabPage_device.Text = "device";
            this.tabPage_device.UseVisualStyleBackColor = true;
            // 
            // checkedListBox_Ex
            // 
            this.checkedListBox_Ex.Dock = System.Windows.Forms.DockStyle.Fill;
            this.checkedListBox_Ex.FormattingEnabled = true;
            this.checkedListBox_Ex.Items.AddRange(new object[] {
            "Select Device"});
            this.checkedListBox_Ex.Location = new System.Drawing.Point(3, 3);
            this.checkedListBox_Ex.Name = "checkedListBox_Ex";
            this.checkedListBox_Ex.Size = new System.Drawing.Size(344, 384);
            this.checkedListBox_Ex.TabIndex = 18;
            this.checkedListBox_Ex.DoubleClick += new System.EventHandler(this.checkedListBox_Ex_DoubleClick);
            // 
            // tabPage_active
            // 
            this.tabPage_active.Controls.Add(this.checkedListBox_My);
            this.tabPage_active.Location = new System.Drawing.Point(4, 22);
            this.tabPage_active.Name = "tabPage_active";
            this.tabPage_active.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage_active.Size = new System.Drawing.Size(350, 390);
            this.tabPage_active.TabIndex = 2;
            this.tabPage_active.Text = "active";
            this.tabPage_active.UseVisualStyleBackColor = true;
            // 
            // checkedListBox_My
            // 
            this.checkedListBox_My.Dock = System.Windows.Forms.DockStyle.Fill;
            this.checkedListBox_My.FormattingEnabled = true;
            this.checkedListBox_My.Items.AddRange(new object[] {
            "Select System"});
            this.checkedListBox_My.Location = new System.Drawing.Point(3, 3);
            this.checkedListBox_My.Name = "checkedListBox_My";
            this.checkedListBox_My.Size = new System.Drawing.Size(344, 384);
            this.checkedListBox_My.TabIndex = 19;
            this.checkedListBox_My.DoubleClick += new System.EventHandler(this.checkedListBox_My_DoubleClick);
            // 
            // tabPage_activeFliter
            // 
            this.tabPage_activeFliter.Controls.Add(this.listBox_find);
            this.tabPage_activeFliter.Location = new System.Drawing.Point(4, 22);
            this.tabPage_activeFliter.Name = "tabPage_activeFliter";
            this.tabPage_activeFliter.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage_activeFliter.Size = new System.Drawing.Size(350, 390);
            this.tabPage_activeFliter.TabIndex = 3;
            this.tabPage_activeFliter.Text = "activeFliter";
            this.tabPage_activeFliter.UseVisualStyleBackColor = true;
            // 
            // listBox_find
            // 
            this.listBox_find.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listBox_find.Font = new System.Drawing.Font("굴림", 12F);
            this.listBox_find.FormattingEnabled = true;
            this.listBox_find.ItemHeight = 16;
            this.listBox_find.Location = new System.Drawing.Point(3, 3);
            this.listBox_find.Name = "listBox_find";
            this.listBox_find.Size = new System.Drawing.Size(344, 384);
            this.listBox_find.TabIndex = 0;
            this.listBox_find.DoubleClick += new System.EventHandler(this.listBox_find_DoubleClick);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.label4);
            this.panel1.Controls.Add(this.textBox_activeFind);
            this.panel1.Controls.Add(this.button_ClearLog);
            this.panel1.Controls.Add(this.button_TestORG);
            this.panel1.Controls.Add(this.comboBox_TestExpr);
            this.panel1.Controls.Add(this.button_TestStart);
            this.panel1.Controls.Add(this.button_Stop);
            this.panel1.Controls.Add(this.comboBox_Segment);
            this.panel1.Controls.Add(this.button_Reset);
            this.panel1.Controls.Add(this.button_Start);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Right;
            this.panel1.Location = new System.Drawing.Point(1146, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(116, 416);
            this.panel1.TabIndex = 12;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(14, 259);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(62, 12);
            this.label4.TabIndex = 12;
            this.label4.Text = "activeFind";
            // 
            // textBox_activeFind
            // 
            this.textBox_activeFind.Location = new System.Drawing.Point(16, 274);
            this.textBox_activeFind.Name = "textBox_activeFind";
            this.textBox_activeFind.Size = new System.Drawing.Size(85, 21);
            this.textBox_activeFind.TabIndex = 11;
            this.textBox_activeFind.TextChanged += new System.EventHandler(this.textBox_activeFind_TextChanged);
            // 
            // button_ClearLog
            // 
            this.button_ClearLog.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button_ClearLog.Location = new System.Drawing.Point(16, 23);
            this.button_ClearLog.Name = "button_ClearLog";
            this.button_ClearLog.Size = new System.Drawing.Size(85, 20);
            this.button_ClearLog.TabIndex = 1;
            this.button_ClearLog.Text = "Clear Log";
            this.button_ClearLog.UseVisualStyleBackColor = true;
            this.button_ClearLog.Click += new System.EventHandler(this.button_ClearLog_Click);
            // 
            // comboBox_TestExpr
            // 
            this.comboBox_TestExpr.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBox_TestExpr.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox_TestExpr.FormattingEnabled = true;
            this.comboBox_TestExpr.Location = new System.Drawing.Point(16, 219);
            this.comboBox_TestExpr.Name = "comboBox_TestExpr";
            this.comboBox_TestExpr.Size = new System.Drawing.Size(85, 20);
            this.comboBox_TestExpr.TabIndex = 10;
            this.comboBox_TestExpr.SelectedIndexChanged += new System.EventHandler(this.comboBox_TestExpr_SelectedIndexChanged);
            // 
            // button_Stop
            // 
            this.button_Stop.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button_Stop.Location = new System.Drawing.Point(16, 107);
            this.button_Stop.Name = "button_Stop";
            this.button_Stop.Size = new System.Drawing.Size(85, 20);
            this.button_Stop.TabIndex = 9;
            this.button_Stop.Text = "TEST 멈춤";
            this.button_Stop.UseVisualStyleBackColor = true;
            this.button_Stop.Click += new System.EventHandler(this.button_Stop_Click);
            // 
            // comboBox_Segment
            // 
            this.comboBox_Segment.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBox_Segment.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox_Segment.FormattingEnabled = true;
            this.comboBox_Segment.Location = new System.Drawing.Point(16, 135);
            this.comboBox_Segment.Name = "comboBox_Segment";
            this.comboBox_Segment.Size = new System.Drawing.Size(85, 20);
            this.comboBox_Segment.TabIndex = 6;
            // 
            // button_Reset
            // 
            this.button_Reset.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button_Reset.Location = new System.Drawing.Point(16, 191);
            this.button_Reset.Name = "button_Reset";
            this.button_Reset.Size = new System.Drawing.Size(85, 20);
            this.button_Reset.TabIndex = 8;
            this.button_Reset.Text = "Reset";
            this.button_Reset.UseVisualStyleBackColor = true;
            this.button_Reset.Click += new System.EventHandler(this.button_reset_Click);
            // 
            // button_Start
            // 
            this.button_Start.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button_Start.Location = new System.Drawing.Point(16, 163);
            this.button_Start.Name = "button_Start";
            this.button_Start.Size = new System.Drawing.Size(85, 20);
            this.button_Start.TabIndex = 7;
            this.button_Start.Text = "Start";
            this.button_Start.UseVisualStyleBackColor = true;
            this.button_Start.Click += new System.EventHandler(this.button_start_Click);
            // 
            // richTextBox_Debug
            // 
            this.richTextBox_Debug.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.richTextBox_Debug.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.richTextBox_Debug.Location = new System.Drawing.Point(3, 0);
            this.richTextBox_Debug.Name = "richTextBox_Debug";
            this.richTextBox_Debug.ReadOnly = true;
            this.richTextBox_Debug.Size = new System.Drawing.Size(779, 416);
            this.richTextBox_Debug.TabIndex = 0;
            this.richTextBox_Debug.Text = "";
            // 
            // progressBar1
            // 
            this.progressBar1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.progressBar1.Location = new System.Drawing.Point(0, 784);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(1262, 26);
            this.progressBar1.TabIndex = 2;
            // 
            // splitter1
            // 
            this.splitter1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.splitter1.Enabled = false;
            this.splitter1.Location = new System.Drawing.Point(0, 781);
            this.splitter1.Name = "splitter1";
            this.splitter1.Size = new System.Drawing.Size(1262, 3);
            this.splitter1.TabIndex = 22;
            this.splitter1.TabStop = false;
            // 
            // FormMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.ClientSize = new System.Drawing.Size(1262, 810);
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
            this.splitContainer3.Panel1.ResumeLayout(false);
            this.splitContainer3.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer3)).EndInit();
            this.splitContainer3.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel1.PerformLayout();
            this.splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
            this.splitContainer2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox_ppt)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox_xls)).EndInit();
            this.splitContainer4.Panel1.ResumeLayout(false);
            this.splitContainer4.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer4)).EndInit();
            this.splitContainer4.ResumeLayout(false);
            this.tabControl1.ResumeLayout(false);
            this.tabPage_system.ResumeLayout(false);
            this.tabPage_device.ResumeLayout(false);
            this.tabPage_active.ResumeLayout(false);
            this.tabPage_activeFliter.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
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
        private System.Windows.Forms.Button button_Reset;
        private System.Windows.Forms.Button button_Start;
        private System.Windows.Forms.ComboBox comboBox_Segment;
        private System.Windows.Forms.Button button_Stop;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox comboBox_System;
        private System.Windows.Forms.ComboBox comboBox_TestExpr;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox comboBox_Device;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.CheckedListBox checkedListBox_My;
        private System.Windows.Forms.CheckedListBox checkedListBox_Ex;
        private System.Windows.Forms.CheckedListBox checkedListBox_sysHMI;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox comboBox_Package;
        private System.Windows.Forms.Button button_CreatePLC;
        private System.Windows.Forms.SplitContainer splitContainer3;
        private System.Windows.Forms.TabControl xtraTabControl_Ex;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage_system;
        private System.Windows.Forms.TabPage tabPage_device;
        private System.Windows.Forms.TabPage tabPage_active;
        private System.Windows.Forms.TabPage tabPage_activeFliter;
        private System.Windows.Forms.ListBox listBox_find;
        private System.Windows.Forms.TextBox textBox_activeFind;
        private System.Windows.Forms.Label label4;
    }
}

