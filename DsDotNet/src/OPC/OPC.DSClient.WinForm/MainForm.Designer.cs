using Opc.Ua.Client.Controls;
using OPC.DSClient.WinForm.UserControl;

namespace OPC.DSClient.WinForm
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
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            fluentDesignFormContainer1 = new DevExpress.XtraBars.FluentDesignSystem.FluentDesignFormContainer();
            nFrame1 = new DevExpress.XtraBars.Navigation.NavigationFrame();
            nPage1 = new DevExpress.XtraBars.Navigation.NavigationPage();
            nPage2 = new DevExpress.XtraBars.Navigation.NavigationPage();
            nPage6 = new DevExpress.XtraBars.Navigation.NavigationPage();
            nPage3 = new DevExpress.XtraBars.Navigation.NavigationPage();
            nPage4 = new DevExpress.XtraBars.Navigation.NavigationPage();
            nPage5 = new DevExpress.XtraBars.Navigation.NavigationPage();
            nPage7 = new DevExpress.XtraBars.Navigation.NavigationPage();
            nPage8 = new DevExpress.XtraBars.Navigation.NavigationPage();
            nPage9 = new DevExpress.XtraBars.Navigation.NavigationPage();
            nPage10 = new DevExpress.XtraBars.Navigation.NavigationPage();
            ucDsSankey1 = new UcDsSankey();
            ucDsTreemap1 = new UcDsTreemap();
            ucDsSunburst1 = new UcDsSunburst();
            ucDsTree1 = new UcDsTree();
            ucDsTable1 = new UcDsTable();
            ucDsHeatmap1 = new UcDsHeatmap();
            ucDsDataGridIO = new UcDsDataGrid();
            ucDsDataGridFlow = new UcDsDataGrid();
            ucDsTextEdit1 = new UcDsTextEdit();
            accordionControl1 = new DevExpress.XtraBars.Navigation.AccordionControl();
            comboBoxEdit_HeatmapScale = new DevExpress.XtraEditors.ComboBoxEdit();
            fluentFormDefaultManager1 = new DevExpress.XtraBars.FluentDesignSystem.FluentFormDefaultManager(components);
            skinDropDownButtonItem1 = new DevExpress.XtraBars.SkinDropDownButtonItem();
            skinPaletteDropDownButtonItem1 = new DevExpress.XtraBars.SkinPaletteDropDownButtonItem();
            skinBarSubItem1 = new DevExpress.XtraBars.SkinBarSubItem();
            ace_Treemap = new DevExpress.XtraBars.Navigation.AccordionControlElement();
            ace_Sunburst = new DevExpress.XtraBars.Navigation.AccordionControlElement();
            ace_Sankey = new DevExpress.XtraBars.Navigation.AccordionControlElement();
            ace_Heatmap = new DevExpress.XtraBars.Navigation.AccordionControlElement();
            accordionControlElement5 = new DevExpress.XtraBars.Navigation.AccordionControlElement();
            ace_DataGridFlow = new DevExpress.XtraBars.Navigation.AccordionControlElement();
            ace_DataGridIO = new DevExpress.XtraBars.Navigation.AccordionControlElement();
            ace_Table = new DevExpress.XtraBars.Navigation.AccordionControlElement();
            ace_Tree = new DevExpress.XtraBars.Navigation.AccordionControlElement();
            ace_TextEdit = new DevExpress.XtraBars.Navigation.AccordionControlElement();
            ace_HMI = new DevExpress.XtraBars.Navigation.AccordionControlElement();
            fluentDesignFormControl1 = new DevExpress.XtraBars.FluentDesignSystem.FluentDesignFormControl();
            checkEdit1 = new DevExpress.XtraEditors.CheckEdit();
            fluentDesignFormContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)nFrame1).BeginInit();
            nFrame1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)accordionControl1).BeginInit();
            accordionControl1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)comboBoxEdit_HeatmapScale.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)fluentFormDefaultManager1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)fluentDesignFormControl1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)checkEdit1.Properties).BeginInit();
            SuspendLayout();
            // 
            // fluentDesignFormContainer1
            // 
            fluentDesignFormContainer1.Controls.Add(nFrame1);
            fluentDesignFormContainer1.Controls.Add(ucDsSankey1);
            fluentDesignFormContainer1.Controls.Add(ucDsTreemap1);
            fluentDesignFormContainer1.Controls.Add(ucDsSunburst1);
            fluentDesignFormContainer1.Controls.Add(ucDsTree1);
            fluentDesignFormContainer1.Controls.Add(ucDsTable1);
            fluentDesignFormContainer1.Controls.Add(ucDsHeatmap1);
            fluentDesignFormContainer1.Dock = DockStyle.Fill;
            fluentDesignFormContainer1.Location = new Point(226, 40);
            fluentDesignFormContainer1.Name = "fluentDesignFormContainer1";
            fluentDesignFormContainer1.Size = new Size(1235, 631);
            fluentDesignFormContainer1.TabIndex = 0;
            // 
            // nFrame1
            // 
            nFrame1.Controls.Add(nPage1);
            nFrame1.Controls.Add(nPage2);
            nFrame1.Controls.Add(nPage6);
            nFrame1.Controls.Add(nPage3);
            nFrame1.Controls.Add(nPage4);
            nFrame1.Controls.Add(nPage5);
            nFrame1.Controls.Add(nPage7);
            nFrame1.Controls.Add(nPage8);
            nFrame1.Controls.Add(nPage9);
            nFrame1.Controls.Add(nPage10);
            nFrame1.Dock = DockStyle.Fill;
            nFrame1.Location = new Point(0, 0);
            nFrame1.Name = "nFrame1";
            nFrame1.Pages.AddRange(new DevExpress.XtraBars.Navigation.NavigationPageBase[] { nPage1, nPage2, nPage3, nPage4, nPage5, nPage6, nPage7, nPage8, nPage9, nPage10 });
            nFrame1.SelectedPage = nPage1;
            nFrame1.Size = new Size(1235, 631);
            nFrame1.TabIndex = 7;
            nFrame1.Text = "navigationFrame1";
            // 
            // nPage1
            // 
            nPage1.Name = "nPage1";
            nPage1.Size = new Size(1235, 631);
            // 
            // nPage2
            // 
            nPage2.Name = "nPage2";
            nPage2.Size = new Size(1235, 631);
            // 
            // nPage6
            // 
            nPage6.Name = "nPage6";
            nPage6.Size = new Size(1235, 631);
            // 
            // nPage3
            // 
            nPage3.Name = "nPage3";
            nPage3.Size = new Size(1235, 631);
            // 
            // nPage4
            // 
            nPage4.Name = "nPage4";
            nPage4.Size = new Size(1235, 631);
            // 
            // nPage5
            // 
            nPage5.Name = "nPage5";
            nPage5.Size = new Size(1235, 631);
            // 
            // nPage7
            // 
            nPage7.Name = "nPage7";
            nPage7.Size = new Size(1235, 631);
            // 
            // nPage8
            // 
            nPage8.Name = "nPage8";
            nPage8.Size = new Size(1235, 631);
            // 
            // nPage9
            // 
            nPage9.Name = "nPage9";
            nPage9.Size = new Size(1235, 631);
            // 
            // nPage10
            // 
            nPage10.Name = "nPage10";
            nPage10.Size = new Size(1235, 631);
            // 
            // ucDsSankey1
            // 
            ucDsSankey1.Dock = DockStyle.Fill;
            ucDsSankey1.Location = new Point(0, 0);
            ucDsSankey1.Name = "ucDsSankey1";
            ucDsSankey1.Size = new Size(1235, 631);
            ucDsSankey1.TabIndex = 1;
            // 
            // ucDsTreemap1
            // 
            ucDsTreemap1.Dock = DockStyle.Fill;
            ucDsTreemap1.Location = new Point(0, 0);
            ucDsTreemap1.Name = "ucDsTreemap1";
            ucDsTreemap1.Size = new Size(1235, 631);
            ucDsTreemap1.TabIndex = 5;
            // 
            // ucDsSunburst1
            // 
            ucDsSunburst1.Dock = DockStyle.Fill;
            ucDsSunburst1.Location = new Point(0, 0);
            ucDsSunburst1.Name = "ucDsSunburst1";
            ucDsSunburst1.Size = new Size(1235, 631);
            ucDsSunburst1.TabIndex = 2;
            // 
            // ucDsTree1
            // 
            ucDsTree1.Dock = DockStyle.Fill;
            ucDsTree1.Location = new Point(0, 0);
            ucDsTree1.Name = "ucDsTree1";
            ucDsTree1.Size = new Size(1235, 631);
            ucDsTree1.TabIndex = 6;
            // 
            // ucDsTable1
            // 
            ucDsTable1.Dock = DockStyle.Fill;
            ucDsTable1.Location = new Point(0, 0);
            ucDsTable1.Name = "ucDsTable1";
            ucDsTable1.Size = new Size(1235, 631);
            ucDsTable1.TabIndex = 3;
            // 
            // ucDsHeatmap1
            // 
            ucDsHeatmap1.Dock = DockStyle.Fill;
            ucDsHeatmap1.Location = new Point(0, 0);
            ucDsHeatmap1.Name = "ucDsHeatmap1";
            ucDsHeatmap1.Size = new Size(1235, 631);
            ucDsHeatmap1.TabIndex = 0;
            // 
            // ucDsDataGridIO
            // 
            ucDsDataGridIO.Dock = DockStyle.Fill;
            ucDsDataGridIO.Location = new Point(0, 0);
            ucDsDataGridIO.Name = "ucDsDataGridIO";
            ucDsDataGridIO.Size = new Size(1249, 635);
            ucDsDataGridIO.TabIndex = 0;
            // 
            // ucDsDataGridFlow
            // 
            ucDsDataGridFlow.Dock = DockStyle.Fill;
            ucDsDataGridFlow.Location = new Point(0, 0);
            ucDsDataGridFlow.Name = "ucDsDataGridFlow";
            ucDsDataGridFlow.Size = new Size(1249, 635);
            ucDsDataGridFlow.TabIndex = 0;
            // 
            // ucDsTextEdit1
            // 
            ucDsTextEdit1.Dock = DockStyle.Fill;
            ucDsTextEdit1.Location = new Point(0, 0);
            ucDsTextEdit1.Name = "ucDsTextEdit1";
            ucDsTextEdit1.Size = new Size(1249, 635);
            ucDsTextEdit1.TabIndex = 8;
            // 
            // accordionControl1
            // 
            accordionControl1.Controls.Add(comboBoxEdit_HeatmapScale);
            accordionControl1.Dock = DockStyle.Left;
            accordionControl1.Elements.AddRange(new DevExpress.XtraBars.Navigation.AccordionControlElement[] { ace_Treemap, ace_Sunburst, ace_Sankey, ace_Heatmap, ace_DataGridFlow, ace_DataGridIO, ace_Table, ace_Tree, ace_TextEdit, ace_HMI });
            accordionControl1.Location = new Point(0, 40);
            accordionControl1.Name = "accordionControl1";
            accordionControl1.ScrollBarMode = DevExpress.XtraBars.Navigation.ScrollBarMode.Touch;
            accordionControl1.Size = new Size(226, 631);
            accordionControl1.TabIndex = 1;
            accordionControl1.ViewType = DevExpress.XtraBars.Navigation.AccordionControlViewType.HamburgerMenu;
            // 
            // comboBoxEdit_HeatmapScale
            // 
            comboBoxEdit_HeatmapScale.Location = new Point(94, 273);
            comboBoxEdit_HeatmapScale.MenuManager = fluentFormDefaultManager1;
            comboBoxEdit_HeatmapScale.Name = "comboBoxEdit_HeatmapScale";
            comboBoxEdit_HeatmapScale.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            comboBoxEdit_HeatmapScale.Size = new Size(100, 32);
            comboBoxEdit_HeatmapScale.TabIndex = 2;
            // 
            // fluentFormDefaultManager1
            // 
            fluentFormDefaultManager1.Form = this;
            fluentFormDefaultManager1.Items.AddRange(new DevExpress.XtraBars.BarItem[] { skinDropDownButtonItem1, skinPaletteDropDownButtonItem1, skinBarSubItem1 });
            fluentFormDefaultManager1.MaxItemId = 3;
            // 
            // skinDropDownButtonItem1
            // 
            skinDropDownButtonItem1.ActAsDropDown = true;
            skinDropDownButtonItem1.ButtonStyle = DevExpress.XtraBars.BarButtonStyle.DropDown;
            skinDropDownButtonItem1.Id = 0;
            skinDropDownButtonItem1.Name = "skinDropDownButtonItem1";
            // 
            // skinPaletteDropDownButtonItem1
            // 
            skinPaletteDropDownButtonItem1.ActAsDropDown = true;
            skinPaletteDropDownButtonItem1.ButtonStyle = DevExpress.XtraBars.BarButtonStyle.DropDown;
            skinPaletteDropDownButtonItem1.Id = 1;
            skinPaletteDropDownButtonItem1.Name = "skinPaletteDropDownButtonItem1";
            // 
            // skinBarSubItem1
            // 
            skinBarSubItem1.Caption = "skinBarSubItem1";
            skinBarSubItem1.Id = 2;
            skinBarSubItem1.Name = "skinBarSubItem1";
            // 
            // ace_Treemap
            // 
            ace_Treemap.Expanded = true;
            ace_Treemap.ImageOptions.SvgImage = (DevExpress.Utils.Svg.SvgImage)resources.GetObject("ace_Treemap.ImageOptions.SvgImage");
            ace_Treemap.Name = "ace_Treemap";
            ace_Treemap.Text = "공정 상태";
            // 
            // ace_Sunburst
            // 
            ace_Sunburst.Expanded = true;
            ace_Sunburst.ImageOptions.SvgImage = (DevExpress.Utils.Svg.SvgImage)resources.GetObject("ace_Sunburst.ImageOptions.SvgImage");
            ace_Sunburst.Name = "ace_Sunburst";
            ace_Sunburst.Text = "이상 보기";
            // 
            // ace_Sankey
            // 
            ace_Sankey.Expanded = true;
            ace_Sankey.ImageOptions.SvgImage = (DevExpress.Utils.Svg.SvgImage)resources.GetObject("ace_Sankey.ImageOptions.SvgImage");
            ace_Sankey.Name = "ace_Sankey";
            ace_Sankey.Text = "동작 순서";
            // 
            // ace_Heatmap
            // 
            ace_Heatmap.Elements.AddRange(new DevExpress.XtraBars.Navigation.AccordionControlElement[] { accordionControlElement5 });
            ace_Heatmap.ImageOptions.SvgImage = (DevExpress.Utils.Svg.SvgImage)resources.GetObject("ace_Heatmap.ImageOptions.SvgImage");
            ace_Heatmap.Name = "ace_Heatmap";
            ace_Heatmap.Text = "편차 분석";
            // 
            // accordionControlElement5
            // 
            accordionControlElement5.HeaderControl = comboBoxEdit_HeatmapScale;
            accordionControlElement5.Name = "accordionControlElement5";
            accordionControlElement5.Style = DevExpress.XtraBars.Navigation.ElementStyle.Item;
            accordionControlElement5.Text = "Scale X";
            // 
            // ace_DataGridFlow
            // 
            ace_DataGridFlow.ImageOptions.SvgImage = (DevExpress.Utils.Svg.SvgImage)resources.GetObject("ace_DataGridFlow.ImageOptions.SvgImage");
            ace_DataGridFlow.Name = "ace_DataGridFlow";
            ace_DataGridFlow.Text = "Flow 모니터링";
            // 
            // ace_DataGridIO
            // 
            ace_DataGridIO.Expanded = true;
            ace_DataGridIO.ImageOptions.SvgImage = (DevExpress.Utils.Svg.SvgImage)resources.GetObject("ace_DataGridIO.ImageOptions.SvgImage");
            ace_DataGridIO.Name = "ace_DataGridIO";
            ace_DataGridIO.Text = "IO 모니터링";
            // 
            // ace_Table
            // 
            ace_Table.Expanded = true;
            ace_Table.ImageOptions.SvgImage = (DevExpress.Utils.Svg.SvgImage)resources.GetObject("ace_Table.ImageOptions.SvgImage");
            ace_Table.Name = "ace_Table";
            ace_Table.Text = "TAG 모니터링";
            // 
            // ace_Tree
            // 
            ace_Tree.Expanded = true;
            ace_Tree.ImageOptions.SvgImage = (DevExpress.Utils.Svg.SvgImage)resources.GetObject("ace_Tree.ImageOptions.SvgImage");
            ace_Tree.Name = "ace_Tree";
            ace_Tree.Text = "모델 구조";
            // 
            // ace_TextEdit
            // 
            ace_TextEdit.Expanded = true;
            ace_TextEdit.ImageOptions.SvgImage = (DevExpress.Utils.Svg.SvgImage)resources.GetObject("ace_TextEdit.ImageOptions.SvgImage");
            ace_TextEdit.Name = "ace_TextEdit";
            ace_TextEdit.Text = "모델 텍스트";
            // 
            // ace_HMI
            // 
            ace_HMI.Expanded = true;
            ace_HMI.ImageOptions.SvgImage = (DevExpress.Utils.Svg.SvgImage)resources.GetObject("ace_HMI.ImageOptions.SvgImage");
            ace_HMI.Name = "ace_HMI";
            ace_HMI.Text = "조작 화면";
            // 
            // fluentDesignFormControl1
            // 
            fluentDesignFormControl1.FluentDesignForm = this;
            fluentDesignFormControl1.Items.AddRange(new DevExpress.XtraBars.BarItem[] { skinDropDownButtonItem1, skinPaletteDropDownButtonItem1, skinBarSubItem1 });
            fluentDesignFormControl1.Location = new Point(0, 0);
            fluentDesignFormControl1.Manager = fluentFormDefaultManager1;
            fluentDesignFormControl1.Name = "fluentDesignFormControl1";
            fluentDesignFormControl1.Size = new Size(1461, 40);
            fluentDesignFormControl1.TabIndex = 2;
            fluentDesignFormControl1.TabStop = false;
            fluentDesignFormControl1.TitleItemLinks.Add(skinPaletteDropDownButtonItem1);
            // 
            // checkEdit1
            // 
            checkEdit1.Location = new Point(119, 273);
            checkEdit1.MenuManager = fluentFormDefaultManager1;
            checkEdit1.Name = "checkEdit1";
            checkEdit1.Properties.Caption = "checkEdit1";
            checkEdit1.Size = new Size(75, 34);
            checkEdit1.TabIndex = 2;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 14F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1461, 671);
            ControlContainer = fluentDesignFormContainer1;
            Controls.Add(fluentDesignFormContainer1);
            Controls.Add(accordionControl1);
            Controls.Add(checkEdit1);
            Controls.Add(fluentDesignFormControl1);
            FluentDesignFormControl = fluentDesignFormControl1;
            IconOptions.Icon = (Icon)resources.GetObject("MainForm.IconOptions.Icon");
            Name = "MainForm";
            NavigationControl = accordionControl1;
            Text = "DS Pilot";
            fluentDesignFormContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)nFrame1).EndInit();
            nFrame1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)accordionControl1).EndInit();
            accordionControl1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)comboBoxEdit_HeatmapScale.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)fluentFormDefaultManager1).EndInit();
            ((System.ComponentModel.ISupportInitialize)fluentDesignFormControl1).EndInit();
            ((System.ComponentModel.ISupportInitialize)checkEdit1.Properties).EndInit();
            ResumeLayout(false);
        }

        #endregion


        private DevExpress.XtraBars.FluentDesignSystem.FluentDesignFormContainer fluentDesignFormContainer1;
        private DevExpress.XtraBars.Navigation.AccordionControl accordionControl1;
        private DevExpress.XtraBars.FluentDesignSystem.FluentDesignFormControl fluentDesignFormControl1;
        private DevExpress.XtraBars.FluentDesignSystem.FluentFormDefaultManager fluentFormDefaultManager1;
        private DevExpress.XtraBars.Navigation.AccordionControlElement ace_Table;
        private DevExpress.XtraBars.Navigation.AccordionControlElement ace_Tree;
        private DevExpress.XtraBars.Navigation.AccordionControlElement ace_Heatmap;
        private DevExpress.XtraBars.Navigation.AccordionControlElement ace_Sunburst;
        private DevExpress.XtraBars.Navigation.AccordionControlElement ace_Treemap;
        private DevExpress.XtraBars.Navigation.AccordionControlElement ace_Sankey;
        private UserControl.UcDsTable ucDsTable1;
        private UserControl.UcDsTreemap ucDsTreemap1;
        private UserControl.UcDsTree ucDsTree1;
        private UserControl.UcDsSunburst ucDsSunburst1;
        private UserControl.UcDsSankey ucDsSankey1;
        private UserControl.UcDsHeatmap ucDsHeatmap1;
        private DevExpress.XtraBars.Navigation.NavigationFrame nFrame1;
        private DevExpress.XtraBars.Navigation.NavigationPage nPage1;
        private DevExpress.XtraBars.Navigation.NavigationPage nPage2;
        private DevExpress.XtraBars.Navigation.NavigationPage nPage3;
        private DevExpress.XtraBars.Navigation.NavigationPage nPage4;
        private DevExpress.XtraBars.Navigation.NavigationPage nPage5;
        private DevExpress.XtraBars.Navigation.NavigationPage nPage6;
        private DevExpress.XtraBars.Navigation.NavigationPage nPage7;
        private DevExpress.XtraBars.Navigation.NavigationPage nPage8;
        private DevExpress.XtraBars.Navigation.NavigationPage nPage9;
        private DevExpress.XtraBars.Navigation.NavigationPage nPage10;
        private DevExpress.XtraBars.Navigation.AccordionControlElement ace_DataGridIO;
        private UserControl.UcDsDataGrid ucDsDataGridIO;
        private UserControl.UcDsDataGrid ucDsDataGridFlow;
        private UserControl.UcDsTextEdit ucDsTextEdit1;
        private DevExpress.XtraBars.SkinDropDownButtonItem skinDropDownButtonItem1;
        private DevExpress.XtraBars.SkinPaletteDropDownButtonItem skinPaletteDropDownButtonItem1;
        private DevExpress.XtraBars.Navigation.AccordionControlElement ace_TextEdit;
        private DevExpress.XtraBars.SkinBarSubItem skinBarSubItem1;
        private DevExpress.XtraBars.Navigation.AccordionControlElement ace_DataGridFlow;
        private DevExpress.XtraBars.Navigation.AccordionControlElement ace_HMI;
        private DevExpress.XtraBars.Navigation.AccordionControlElement accordionControlElement5;
        private DevExpress.XtraEditors.ComboBoxEdit comboBoxEdit_HeatmapScale;
        private DevExpress.XtraEditors.CheckEdit checkEdit1;
    }
}