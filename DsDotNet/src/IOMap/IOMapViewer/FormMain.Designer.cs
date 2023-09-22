
namespace IOMapViewer
{
    partial class FormMain
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

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormMain));
            document1 = new DevExpress.XtraBars.Docking2010.Views.Tabbed.Document(components);
            dockManager = new DevExpress.XtraBars.Docking.DockManager(components);
            barManager1 = new BarManager(components);
            bar3 = new Bar();
            skinPaletteDropDownButtonItem1 = new SkinPaletteDropDownButtonItem();
            barStaticItem_logCnt = new BarStaticItem();
            barEditItem_Process = new BarEditItem();
            repositoryItemProgressBar1 = new DevExpress.XtraEditors.Repository.RepositoryItemProgressBar();
            barStaticItem_procText = new BarStaticItem();
            barDockControlTop = new BarDockControl();
            barDockControlBottom = new BarDockControl();
            barDockControlLeft = new BarDockControl();
            barDockControlRight = new BarDockControl();
            barStaticItem1 = new BarStaticItem();
            barStaticItem2 = new BarStaticItem();
            panelContainer1 = new DevExpress.XtraBars.Docking.DockPanel();
            dockPanel1 = new DevExpress.XtraBars.Docking.DockPanel();
            dockPanel1_Container = new DevExpress.XtraBars.Docking.ControlContainer();
            dockPanel2 = new DevExpress.XtraBars.Docking.DockPanel();
            dockPanel2_Container = new DevExpress.XtraBars.Docking.ControlContainer();
            ucLog1 = new UcLog();
            documentManager = new DevExpress.XtraBars.Docking2010.DocumentManager(components);
            tabbedView_doc = new TabbedView(components);
            document3 = new DevExpress.XtraBars.Docking2010.Views.Tabbed.Document(components);
            documentGroup1 = new DocumentGroup(components);
            ac_Main = new AccordionControl();
            accordionContentContainer2 = new AccordionContentContainer();
            simpleButton_layoutReset = new SimpleButton();
            ace_Memory = new AccordionControlElement();
            ace_Setting = new AccordionControlElement();
            ace9 = new AccordionControlElement();
            gridControl_exprotExcel = new GridControl();
            gridView_exprotExcel = new GridView();
            ((System.ComponentModel.ISupportInitialize)document1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dockManager).BeginInit();
            ((System.ComponentModel.ISupportInitialize)barManager1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)repositoryItemProgressBar1).BeginInit();
            panelContainer1.SuspendLayout();
            dockPanel1.SuspendLayout();
            dockPanel2.SuspendLayout();
            dockPanel2_Container.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)documentManager).BeginInit();
            ((System.ComponentModel.ISupportInitialize)tabbedView_doc).BeginInit();
            ((System.ComponentModel.ISupportInitialize)document3).BeginInit();
            ((System.ComponentModel.ISupportInitialize)documentGroup1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)ac_Main).BeginInit();
            ac_Main.SuspendLayout();
            accordionContentContainer2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)gridControl_exprotExcel).BeginInit();
            ((System.ComponentModel.ISupportInitialize)gridView_exprotExcel).BeginInit();
            SuspendLayout();
            // 
            // document1
            // 
            document1.Caption = "dockPanel1";
            document1.ControlName = "dockPanel1";
            document1.FloatLocation = new Point(687, 414);
            document1.FloatSize = new Size(200, 200);
            document1.Properties.AllowClose = DevExpress.Utils.DefaultBoolean.True;
            document1.Properties.AllowFloat = DevExpress.Utils.DefaultBoolean.True;
            document1.Properties.AllowFloatOnDoubleClick = DevExpress.Utils.DefaultBoolean.True;
            // 
            // dockManager
            // 
            dockManager.Form = this;
            dockManager.MenuManager = barManager1;
            dockManager.RootPanels.AddRange(new DevExpress.XtraBars.Docking.DockPanel[] { panelContainer1 });
            dockManager.TopZIndexControls.AddRange(new string[] { "DevExpress.XtraBars.BarDockControl", "DevExpress.XtraBars.StandaloneBarDockControl", "System.Windows.Forms.StatusBar", "System.Windows.Forms.MenuStrip", "System.Windows.Forms.StatusStrip", "DevExpress.XtraBars.Ribbon.RibbonStatusBar", "DevExpress.XtraBars.Ribbon.RibbonControl", "DevExpress.XtraBars.Navigation.OfficeNavigationBar", "DevExpress.XtraBars.Navigation.TileNavPane" });
            // 
            // barManager1
            // 
            barManager1.Bars.AddRange(new Bar[] { bar3 });
            barManager1.DockControls.Add(barDockControlTop);
            barManager1.DockControls.Add(barDockControlBottom);
            barManager1.DockControls.Add(barDockControlLeft);
            barManager1.DockControls.Add(barDockControlRight);
            barManager1.DockManager = dockManager;
            barManager1.Form = this;
            barManager1.Items.AddRange(new BarItem[] { barStaticItem1, skinPaletteDropDownButtonItem1, barEditItem_Process, barStaticItem_procText, barStaticItem_logCnt, barStaticItem2 });
            barManager1.MaxItemId = 6;
            barManager1.RepositoryItems.AddRange(new DevExpress.XtraEditors.Repository.RepositoryItem[] { repositoryItemProgressBar1 });
            barManager1.StatusBar = bar3;
            // 
            // bar3
            // 
            bar3.BarName = "Status bar";
            bar3.CanDockStyle = BarCanDockStyle.Bottom;
            bar3.DockCol = 0;
            bar3.DockRow = 0;
            bar3.DockStyle = BarDockStyle.Bottom;
            bar3.LinksPersistInfo.AddRange(new LinkPersistInfo[] { new LinkPersistInfo(skinPaletteDropDownButtonItem1), new LinkPersistInfo(barStaticItem_logCnt), new LinkPersistInfo(BarLinkUserDefines.Width, barEditItem_Process, "", false, true, true, 1347), new LinkPersistInfo(barStaticItem_procText) });
            bar3.OptionsBar.AllowQuickCustomization = false;
            bar3.OptionsBar.DrawDragBorder = false;
            bar3.OptionsBar.UseWholeRow = true;
            bar3.Text = "Status bar";
            // 
            // skinPaletteDropDownButtonItem1
            // 
            skinPaletteDropDownButtonItem1.ActAsDropDown = true;
            skinPaletteDropDownButtonItem1.ButtonStyle = BarButtonStyle.DropDown;
            skinPaletteDropDownButtonItem1.Id = 1;
            skinPaletteDropDownButtonItem1.Name = "skinPaletteDropDownButtonItem1";
            // 
            // barStaticItem_logCnt
            // 
            barStaticItem_logCnt.Id = 5;
            barStaticItem_logCnt.Name = "barStaticItem_logCnt";
            // 
            // barEditItem_Process
            // 
            barEditItem_Process.AutoFillWidth = true;
            barEditItem_Process.Caption = "Prcess";
            barEditItem_Process.Edit = repositoryItemProgressBar1;
            barEditItem_Process.Id = 2;
            barEditItem_Process.Name = "barEditItem_Process";
            // 
            // repositoryItemProgressBar1
            // 
            repositoryItemProgressBar1.Name = "repositoryItemProgressBar1";
            // 
            // barStaticItem_procText
            // 
            barStaticItem_procText.Caption = "0%";
            barStaticItem_procText.Id = 3;
            barStaticItem_procText.Name = "barStaticItem_procText";
            // 
            // barDockControlTop
            // 
            barDockControlTop.CausesValidation = false;
            barDockControlTop.Dock = DockStyle.Top;
            barDockControlTop.Location = new Point(0, 0);
            barDockControlTop.Manager = barManager1;
            barDockControlTop.Size = new Size(1463, 0);
            // 
            // barDockControlBottom
            // 
            barDockControlBottom.CausesValidation = false;
            barDockControlBottom.Dock = DockStyle.Bottom;
            barDockControlBottom.Location = new Point(0, 862);
            barDockControlBottom.Manager = barManager1;
            barDockControlBottom.Size = new Size(1463, 27);
            // 
            // barDockControlLeft
            // 
            barDockControlLeft.CausesValidation = false;
            barDockControlLeft.Dock = DockStyle.Left;
            barDockControlLeft.Location = new Point(0, 0);
            barDockControlLeft.Manager = barManager1;
            barDockControlLeft.Size = new Size(0, 862);
            // 
            // barDockControlRight
            // 
            barDockControlRight.CausesValidation = false;
            barDockControlRight.Dock = DockStyle.Right;
            barDockControlRight.Location = new Point(1463, 0);
            barDockControlRight.Manager = barManager1;
            barDockControlRight.Size = new Size(0, 862);
            // 
            // barStaticItem1
            // 
            barStaticItem1.Caption = "Ready";
            barStaticItem1.Id = 0;
            barStaticItem1.Name = "barStaticItem1";
            // 
            // barStaticItem2
            // 
            barStaticItem2.Caption = "barStaticItem2";
            barStaticItem2.Id = 4;
            barStaticItem2.Name = "barStaticItem2";
            // 
            // panelContainer1
            // 
            panelContainer1.Controls.Add(dockPanel1);
            panelContainer1.Controls.Add(dockPanel2);
            panelContainer1.Dock = DevExpress.XtraBars.Docking.DockingStyle.Right;
            panelContainer1.ID = new Guid("65bd18dd-cacd-46d2-9f60-cc9be65c8417");
            panelContainer1.Location = new Point(1258, 0);
            panelContainer1.Name = "panelContainer1";
            panelContainer1.OriginalSize = new Size(205, 200);
            panelContainer1.Size = new Size(205, 862);
            panelContainer1.Text = "panelContainer1";
            // 
            // dockPanel1
            // 
            dockPanel1.Controls.Add(dockPanel1_Container);
            dockPanel1.Dock = DevExpress.XtraBars.Docking.DockingStyle.Fill;
            dockPanel1.ID = new Guid("b3654017-7ce0-4059-b43d-80f3ec910dba");
            dockPanel1.Location = new Point(0, 0);
            dockPanel1.Margin = new Padding(3, 2, 3, 2);
            dockPanel1.Name = "dockPanel1";
            dockPanel1.OriginalSize = new Size(205, 404);
            dockPanel1.Size = new Size(205, 432);
            dockPanel1.Text = "Property";
            // 
            // dockPanel1_Container
            // 
            dockPanel1_Container.Location = new Point(4, 26);
            dockPanel1_Container.Margin = new Padding(3, 2, 3, 2);
            dockPanel1_Container.Name = "dockPanel1_Container";
            dockPanel1_Container.Size = new Size(198, 402);
            dockPanel1_Container.TabIndex = 0;
            // 
            // dockPanel2
            // 
            dockPanel2.Controls.Add(dockPanel2_Container);
            dockPanel2.Dock = DevExpress.XtraBars.Docking.DockingStyle.Fill;
            dockPanel2.FloatVertical = true;
            dockPanel2.ID = new Guid("e2b1525a-f97a-48d8-a364-d6bb5144ac35");
            dockPanel2.Location = new Point(0, 432);
            dockPanel2.Margin = new Padding(3, 2, 3, 2);
            dockPanel2.Name = "dockPanel2";
            dockPanel2.OriginalSize = new Size(205, 402);
            dockPanel2.Size = new Size(205, 430);
            dockPanel2.Text = "Log";
            // 
            // dockPanel2_Container
            // 
            dockPanel2_Container.Controls.Add(ucLog1);
            dockPanel2_Container.Location = new Point(4, 26);
            dockPanel2_Container.Margin = new Padding(3, 2, 3, 2);
            dockPanel2_Container.Name = "dockPanel2_Container";
            dockPanel2_Container.Size = new Size(198, 401);
            dockPanel2_Container.TabIndex = 0;
            // 
            // ucLog1
            // 
            ucLog1.Dock = DockStyle.Fill;
            ucLog1.Location = new Point(0, 0);
            ucLog1.Margin = new Padding(2);
            ucLog1.Name = "ucLog1";
            ucLog1.SelectedIndex = -1;
            ucLog1.Size = new Size(198, 401);
            ucLog1.TabIndex = 1;
            ucLog1.TrackEndOfLine = true;
            // 
            // documentManager
            // 
            documentManager.MdiParent = this;
            documentManager.MenuManager = barManager1;
            documentManager.RibbonAndBarsMergeStyle = RibbonAndBarsMergeStyle.Always;
            documentManager.View = tabbedView_doc;
            documentManager.ViewCollection.AddRange(new BaseView[] { tabbedView_doc });
            // 
            // document3
            // 
            document3.Caption = "document2";
            document3.ControlName = "document2";
            document3.FloatLocation = new Point(393, 222);
            document3.FloatSize = new Size(810, 284);
            // 
            // documentGroup1
            // 
            documentGroup1.Items.AddRange(new DevExpress.XtraBars.Docking2010.Views.Tabbed.Document[] { document1, document3 });
            // 
            // ac_Main
            // 
            ac_Main.Controls.Add(accordionContentContainer2);
            ac_Main.Dock = DockStyle.Left;
            ac_Main.Elements.AddRange(new AccordionControlElement[] { ace_Memory, ace_Setting });
            ac_Main.Location = new Point(0, 0);
            ac_Main.Name = "ac_Main";
            ac_Main.ScrollBarMode = ScrollBarMode.Touch;
            ac_Main.Size = new Size(328, 862);
            ac_Main.TabIndex = 22;
            ac_Main.ViewType = AccordionControlViewType.HamburgerMenu;
            // 
            // accordionContentContainer2
            // 
            accordionContentContainer2.Controls.Add(simpleButton_layoutReset);
            accordionContentContainer2.Name = "accordionContentContainer2";
            accordionContentContainer2.Size = new Size(309, 61);
            accordionContentContainer2.TabIndex = 15;
            // 
            // simpleButton_layoutReset
            // 
            simpleButton_layoutReset.Appearance.Font = new Font("Tahoma", 12F, FontStyle.Regular, GraphicsUnit.Point);
            simpleButton_layoutReset.Appearance.Options.UseFont = true;
            simpleButton_layoutReset.ImageOptions.ImageToTextAlignment = ImageAlignToText.RightCenter;
            simpleButton_layoutReset.ImageOptions.SvgImage = (DevExpress.Utils.Svg.SvgImage)resources.GetObject("simpleButton_layoutReset.ImageOptions.SvgImage");
            simpleButton_layoutReset.Location = new Point(35, 13);
            simpleButton_layoutReset.Name = "simpleButton_layoutReset";
            simpleButton_layoutReset.PaintStyle = PaintStyles.Light;
            simpleButton_layoutReset.Size = new Size(103, 29);
            simpleButton_layoutReset.TabIndex = 0;
            simpleButton_layoutReset.Text = "창 복원";
            // 
            // ace_Memory
            // 
            ace_Memory.Appearance.Default.Font = new Font("Tahoma", 12F, FontStyle.Regular, GraphicsUnit.Point);
            ace_Memory.Appearance.Default.Options.UseFont = true;
            ace_Memory.ImageOptions.SvgImage = (DevExpress.Utils.Svg.SvgImage)resources.GetObject("ace_Memory.ImageOptions.SvgImage");
            ace_Memory.Name = "ace_Memory";
            ace_Memory.Text = "MEMORY";
            // 
            // ace_Setting
            // 
            ace_Setting.Appearance.Default.Font = new Font("Tahoma", 12F, FontStyle.Regular, GraphicsUnit.Point);
            ace_Setting.Appearance.Default.Options.UseFont = true;
            ace_Setting.Elements.AddRange(new AccordionControlElement[] { ace9 });
            ace_Setting.Expanded = true;
            ace_Setting.ImageOptions.SvgImage = (DevExpress.Utils.Svg.SvgImage)resources.GetObject("ace_Setting.ImageOptions.SvgImage");
            ace_Setting.Name = "ace_Setting";
            ace_Setting.Text = "SETTING";
            // 
            // ace9
            // 
            ace9.ContentContainer = accordionContentContainer2;
            ace9.ImageOptions.SvgImage = (DevExpress.Utils.Svg.SvgImage)resources.GetObject("ace9.ImageOptions.SvgImage");
            ace9.Name = "ace9";
            ace9.Style = ElementStyle.Item;
            ace9.Text = "Layout";
            // 
            // gridControl_exprotExcel
            // 
            gridControl_exprotExcel.Location = new Point(630, 557);
            gridControl_exprotExcel.MainView = gridView_exprotExcel;
            gridControl_exprotExcel.MenuManager = barManager1;
            gridControl_exprotExcel.Name = "gridControl_exprotExcel";
            gridControl_exprotExcel.Size = new Size(400, 200);
            gridControl_exprotExcel.TabIndex = 28;
            gridControl_exprotExcel.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] { gridView_exprotExcel });
            gridControl_exprotExcel.Visible = false;
            // 
            // gridView_exprotExcel
            // 
            gridView_exprotExcel.GridControl = gridControl_exprotExcel;
            gridView_exprotExcel.Name = "gridView_exprotExcel";
            // 
            // FormMain
            // 
            Appearance.Options.UseFont = true;
            AutoScaleDimensions = new SizeF(7F, 14F);
            AutoScaleMode = AutoScaleMode.Font;
            AutoSize = true;
            ClientSize = new Size(1463, 889);
            Controls.Add(gridControl_exprotExcel);
            Controls.Add(ac_Main);
            Controls.Add(panelContainer1);
            Controls.Add(barDockControlLeft);
            Controls.Add(barDockControlRight);
            Controls.Add(barDockControlBottom);
            Controls.Add(barDockControlTop);
            IconOptions.Image = (Image)resources.GetObject("FormMain.IconOptions.Image");
            IsMdiContainer = true;
            Margin = new Padding(4, 6, 4, 6);
            Name = "FormMain";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Dualsoft";
            FormClosing += FormMain_FormClosing;
            Load += FormMain_Load;
            Shown += FormMain_Shown;
            ((System.ComponentModel.ISupportInitialize)document1).EndInit();
            ((System.ComponentModel.ISupportInitialize)dockManager).EndInit();
            ((System.ComponentModel.ISupportInitialize)barManager1).EndInit();
            ((System.ComponentModel.ISupportInitialize)repositoryItemProgressBar1).EndInit();
            panelContainer1.ResumeLayout(false);
            dockPanel1.ResumeLayout(false);
            dockPanel2.ResumeLayout(false);
            dockPanel2_Container.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)documentManager).EndInit();
            ((System.ComponentModel.ISupportInitialize)tabbedView_doc).EndInit();
            ((System.ComponentModel.ISupportInitialize)document3).EndInit();
            ((System.ComponentModel.ISupportInitialize)documentGroup1).EndInit();
            ((System.ComponentModel.ISupportInitialize)ac_Main).EndInit();
            ac_Main.ResumeLayout(false);
            accordionContentContainer2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)gridControl_exprotExcel).EndInit();
            ((System.ComponentModel.ISupportInitialize)gridView_exprotExcel).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private DevExpress.XtraBars.Docking.DockManager dockManager;
        private DevExpress.XtraBars.Docking2010.DocumentManager documentManager;
        private DevExpress.XtraBars.Docking.DockPanel dockPanel1;
        private DevExpress.XtraBars.Docking.ControlContainer dockPanel1_Container;
        private DevExpress.XtraBars.Docking.DockPanel dockPanel2;
        private DevExpress.XtraBars.Docking.ControlContainer dockPanel2_Container;
        private DevExpress.XtraBars.Docking2010.Views.Tabbed.Document document1;
        private DevExpress.XtraBars.Docking2010.Views.Tabbed.Document document3;
        private DocumentGroup documentGroup1;
        private BarDockControl barDockControlLeft;
        private BarManager barManager1;
        private Bar bar3;
        private BarStaticItem barStaticItem1;
        private BarDockControl barDockControlTop;
        private BarDockControl barDockControlBottom;
        private BarDockControl barDockControlRight;
        private AccordionControl ac_Main;
        private SkinPaletteDropDownButtonItem skinPaletteDropDownButtonItem1;
        private BarEditItem barEditItem_Process;
        private DevExpress.XtraEditors.Repository.RepositoryItemProgressBar repositoryItemProgressBar1;
        private AccordionControlElement ace_Setting;
        private AccordionControlElement ace9;
        private AccordionControlElement ace_Memory;
        private UcLog ucLog1;
        private DevExpress.XtraBars.Docking.DockPanel panelContainer1;
        private BarStaticItem barStaticItem_procText;
        private AccordionContentContainer accordionContentContainer2;
        private SimpleButton simpleButton_layoutReset;
        private BarStaticItem barStaticItem2;
        private GridControl gridControl_exprotExcel;
        private GridView gridView_exprotExcel;
        private TabbedView tabbedView_doc;
        private BarStaticItem barStaticItem_logCnt;

    }
}