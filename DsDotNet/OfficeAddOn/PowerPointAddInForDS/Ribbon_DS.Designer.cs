namespace PowerPointAddInForDS
{
    partial class Ribbon_DS : Microsoft.Office.Tools.Ribbon.RibbonBase
    {
        /// <summary>
        /// 필수 디자이너 변수입니다.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        public Ribbon_DS()
            : base(Globals.Factory.GetRibbonFactory())
        {
            InitializeComponent();
        }

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

        #region 구성 요소 디자이너에서 생성한 코드

        /// <summary>
        /// 디자이너 지원에 필요한 메서드입니다. 
        /// 이 메서드의 내용을 코드 편집기로 수정하지 마세요.
        /// </summary>
        private void InitializeComponent()
        {
            Microsoft.Office.Tools.Ribbon.RibbonDialogLauncher ribbonDialogLauncherImpl1 = this.Factory.CreateRibbonDialogLauncher();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Ribbon_DS));
            this.tab1 = this.Factory.CreateRibbonTab();
            this.group1 = this.Factory.CreateRibbonGroup();
            this.button_checkDS = this.Factory.CreateRibbonButton();
            this.button_showDSDiagram = this.Factory.CreateRibbonButton();
            this.button_showDSText = this.Factory.CreateRibbonButton();
            this.tab1.SuspendLayout();
            this.group1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tab1
            // 
            this.tab1.ControlId.ControlIdType = Microsoft.Office.Tools.Ribbon.RibbonControlIdType.Office;
            this.tab1.Groups.Add(this.group1);
            this.tab1.Label = "TabAddIns";
            this.tab1.Name = "tab1";
            // 
            // group1
            // 
            this.group1.DialogLauncher = ribbonDialogLauncherImpl1;
            this.group1.Items.Add(this.button_checkDS);
            this.group1.Items.Add(this.button_showDSDiagram);
            this.group1.Items.Add(this.button_showDSText);
            this.group1.Label = "Dualsoft ® ";
            this.group1.Name = "group1";
            // 
            // button_checkDS
            // 
            this.button_checkDS.Description = "Check Dualsoft Language";
            this.button_checkDS.Image = ((System.Drawing.Image)(resources.GetObject("button_checkDS.Image")));
            this.button_checkDS.ImageName = "DS";
            this.button_checkDS.Label = "Check Language";
            this.button_checkDS.Name = "button_checkDS";
            this.button_checkDS.ShowImage = true;
            this.button_checkDS.SuperTip = "Check Dualsoft Language";
            this.button_checkDS.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.button_checkDS_Click);
            // 
            // button_showDSDiagram
            // 
            this.button_showDSDiagram.Description = "Check Dualsoft Language";
            this.button_showDSDiagram.Image = ((System.Drawing.Image)(resources.GetObject("button_showDSDiagram.Image")));
            this.button_showDSDiagram.ImageName = "DS";
            this.button_showDSDiagram.Label = "Show Diagram";
            this.button_showDSDiagram.Name = "button_showDSDiagram";
            this.button_showDSDiagram.ShowImage = true;
            this.button_showDSDiagram.SuperTip = "Show Dualsoft Diagram";
            this.button_showDSDiagram.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.button_showDSDiagram_Click);
            // 
            // button_showDSText
            // 
            this.button_showDSText.Description = "Check Dualsoft Language";
            this.button_showDSText.Image = ((System.Drawing.Image)(resources.GetObject("button_showDSText.Image")));
            this.button_showDSText.ImageName = "DS";
            this.button_showDSText.Label = "Show DS Text";
            this.button_showDSText.Name = "button_showDSText";
            this.button_showDSText.ShowImage = true;
            this.button_showDSText.SuperTip = "Show Dualsoft Language Text";
            this.button_showDSText.Click += new Microsoft.Office.Tools.Ribbon.RibbonControlEventHandler(this.button_showDSText_Click);
            // 
            // Ribbon_DS
            // 
            this.Name = "Ribbon_DS";
            this.RibbonType = "Microsoft.PowerPoint.Presentation";
            this.Tabs.Add(this.tab1);
            this.Load += new Microsoft.Office.Tools.Ribbon.RibbonUIEventHandler(this.Ribbon_DS_Load);
            this.tab1.ResumeLayout(false);
            this.tab1.PerformLayout();
            this.group1.ResumeLayout(false);
            this.group1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        internal Microsoft.Office.Tools.Ribbon.RibbonTab tab1;
        internal Microsoft.Office.Tools.Ribbon.RibbonGroup group1;
        private Microsoft.Office.Tools.Ribbon.RibbonButton button_checkDS;
        private Microsoft.Office.Tools.Ribbon.RibbonButton button_showDSDiagram;
        private Microsoft.Office.Tools.Ribbon.RibbonButton button_showDSText;
    }

    partial class ThisRibbonCollection
    {
        internal Ribbon_DS Ribbon1
        {
            get { return this.GetRibbon<Ribbon_DS>(); }
        }
    }
}
