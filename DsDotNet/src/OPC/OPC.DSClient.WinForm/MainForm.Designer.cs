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
            navigationFrame1 = new DevExpress.XtraBars.Navigation.NavigationFrame();
            navigationPage1 = new DevExpress.XtraBars.Navigation.NavigationPage();
            navigationPage2 = new DevExpress.XtraBars.Navigation.NavigationPage();
            navigationPage6 = new DevExpress.XtraBars.Navigation.NavigationPage();
            navigationPage3 = new DevExpress.XtraBars.Navigation.NavigationPage();
            navigationPage4 = new DevExpress.XtraBars.Navigation.NavigationPage();
            navigationPage5 = new DevExpress.XtraBars.Navigation.NavigationPage();
            navigationPage7 = new DevExpress.XtraBars.Navigation.NavigationPage();
            ucDsDataGrid1 = new UserControl.UcDsDataGrid();
            ucDsTextEdit1 = new UserControl.UcDsTextEdit();
            navigationPage8 = new DevExpress.XtraBars.Navigation.NavigationPage();
            ucDsSankey1 = new UserControl.UcDsSankey();
            ucDsTreemap1 = new UserControl.UcDsTreemap();
            ucDsSunburst1 = new UserControl.UcDsSunburst();
            ucDsTree1 = new UserControl.UcDsTree();
            ucDsTable1 = new UserControl.UcDsTable();
            ucDsHeatmap1 = new UserControl.UcDsHeatmap();
            accordionControl1 = new DevExpress.XtraBars.Navigation.AccordionControl();
            ace_Treemap = new DevExpress.XtraBars.Navigation.AccordionControlElement();
            ace_Sunburst = new DevExpress.XtraBars.Navigation.AccordionControlElement();
            ace_Sankey = new DevExpress.XtraBars.Navigation.AccordionControlElement();
            ace_Heatmap = new DevExpress.XtraBars.Navigation.AccordionControlElement();
            ace_DataGrid = new DevExpress.XtraBars.Navigation.AccordionControlElement();
            ace_Table = new DevExpress.XtraBars.Navigation.AccordionControlElement();
            ace_Tree = new DevExpress.XtraBars.Navigation.AccordionControlElement();
            ace_TextEdit = new DevExpress.XtraBars.Navigation.AccordionControlElement();
            fluentDesignFormControl1 = new DevExpress.XtraBars.FluentDesignSystem.FluentDesignFormControl();
            skinDropDownButtonItem1 = new DevExpress.XtraBars.SkinDropDownButtonItem();
            skinPaletteDropDownButtonItem1 = new DevExpress.XtraBars.SkinPaletteDropDownButtonItem();
            fluentFormDefaultManager1 = new DevExpress.XtraBars.FluentDesignSystem.FluentFormDefaultManager(components);
            fluentDesignFormContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)navigationFrame1).BeginInit();
            navigationFrame1.SuspendLayout();
            navigationPage7.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)accordionControl1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)fluentDesignFormControl1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)fluentFormDefaultManager1).BeginInit();
            SuspendLayout();
            // 
            // fluentDesignFormContainer1
            // 
            fluentDesignFormContainer1.Controls.Add(navigationFrame1);
            fluentDesignFormContainer1.Controls.Add(ucDsSankey1);
            fluentDesignFormContainer1.Controls.Add(ucDsTreemap1);
            fluentDesignFormContainer1.Controls.Add(ucDsSunburst1);
            fluentDesignFormContainer1.Controls.Add(ucDsTree1);
            fluentDesignFormContainer1.Controls.Add(ucDsTable1);
            fluentDesignFormContainer1.Controls.Add(ucDsHeatmap1);
            fluentDesignFormContainer1.Dock = DockStyle.Fill;
            fluentDesignFormContainer1.Location = new Point(226, 41);
            fluentDesignFormContainer1.Name = "fluentDesignFormContainer1";
            fluentDesignFormContainer1.Size = new Size(1249, 635);
            fluentDesignFormContainer1.TabIndex = 0;
            // 
            // navigationFrame1
            // 
            navigationFrame1.Controls.Add(navigationPage1);
            navigationFrame1.Controls.Add(navigationPage2);
            navigationFrame1.Controls.Add(navigationPage6);
            navigationFrame1.Controls.Add(navigationPage3);
            navigationFrame1.Controls.Add(navigationPage4);
            navigationFrame1.Controls.Add(navigationPage5);
            navigationFrame1.Controls.Add(navigationPage7);
            navigationFrame1.Controls.Add(navigationPage8);
            navigationFrame1.Dock = DockStyle.Fill;
            navigationFrame1.Location = new Point(0, 0);
            navigationFrame1.Name = "navigationFrame1";
            navigationFrame1.Pages.AddRange(new DevExpress.XtraBars.Navigation.NavigationPageBase[] { navigationPage1, navigationPage2, navigationPage3, navigationPage4, navigationPage5, navigationPage6, navigationPage7, navigationPage8 });
            navigationFrame1.SelectedPage = navigationPage1;
            navigationFrame1.Size = new Size(1249, 635);
            navigationFrame1.TabIndex = 7;
            navigationFrame1.Text = "navigationFrame1";
            // 
            // navigationPage1
            // 
            navigationPage1.Name = "navigationPage1";
            navigationPage1.Size = new Size(1249, 635);
            // 
            // navigationPage2
            // 
            navigationPage2.Name = "navigationPage2";
            navigationPage2.Size = new Size(1249, 635);
            // 
            // navigationPage6
            // 
            navigationPage6.Name = "navigationPage6";
            navigationPage6.Size = new Size(1249, 635);
            // 
            // navigationPage3
            // 
            navigationPage3.Name = "navigationPage3";
            navigationPage3.Size = new Size(1249, 635);
            // 
            // navigationPage4
            // 
            navigationPage4.Name = "navigationPage4";
            navigationPage4.Size = new Size(1249, 635);
            // 
            // navigationPage5
            // 
            navigationPage5.Name = "navigationPage5";
            navigationPage5.Size = new Size(1249, 635);
            // 
            // navigationPage7
            // 
            navigationPage7.Name = "navigationPage7";
            navigationPage7.Size = new Size(1249, 635);
            // 
            // ucDsDataGrid1
            // 
            ucDsDataGrid1.Dock = DockStyle.Fill;
            ucDsDataGrid1.Location = new Point(0, 0);
            ucDsDataGrid1.Name = "ucDsDataGrid1";
            ucDsDataGrid1.Size = new Size(1249, 635);
            ucDsDataGrid1.TabIndex = 0;
            // 
            // navigationPage8
            // 
            navigationPage8.Name = "navigationPage8";
            navigationPage8.Size = new Size(1249, 635);
            // 
            // ucDsSankey1
            // 
            ucDsSankey1.Dock = DockStyle.Fill;
            ucDsSankey1.Location = new Point(0, 0);
            ucDsSankey1.Name = "ucDsSankey1";
            ucDsSankey1.Size = new Size(1249, 635);
            ucDsSankey1.TabIndex = 1;
            // 
            // ucDsTreemap1
            // 
            ucDsTreemap1.Dock = DockStyle.Fill;
            ucDsTreemap1.Location = new Point(0, 0);
            ucDsTreemap1.Name = "ucDsTreemap1";
            ucDsTreemap1.Size = new Size(1249, 635);
            ucDsTreemap1.TabIndex = 5;
            // 
            // ucDsSunburst1
            // 
            ucDsSunburst1.Dock = DockStyle.Fill;
            ucDsSunburst1.Location = new Point(0, 0);
            ucDsSunburst1.Name = "ucDsSunburst1";
            ucDsSunburst1.Size = new Size(1249, 635);
            ucDsSunburst1.TabIndex = 2;
            // 
            // ucDsTree1
            // 
            ucDsTree1.Dock = DockStyle.Fill;
            ucDsTree1.Location = new Point(0, 0);
            ucDsTree1.Name = "ucDsTree1";
            ucDsTree1.Size = new Size(1249, 635);
            ucDsTree1.TabIndex = 6;
            // 
            // ucDsTable1
            // 
            ucDsTable1.Dock = DockStyle.Fill;
            ucDsTable1.Location = new Point(0, 0);
            ucDsTable1.Name = "ucDsTable1";
            ucDsTable1.Size = new Size(1249, 635);
            ucDsTable1.TabIndex = 3;
            // 
            // ucDsHeatmap1
            // 
            ucDsHeatmap1.Dock = DockStyle.Fill;
            ucDsHeatmap1.Location = new Point(0, 0);
            ucDsHeatmap1.Name = "ucDsHeatmap1";
            ucDsHeatmap1.Size = new Size(1249, 635);
            ucDsHeatmap1.TabIndex = 0;
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
            accordionControl1.Dock = DockStyle.Left;
            accordionControl1.Elements.AddRange(new DevExpress.XtraBars.Navigation.AccordionControlElement[] { ace_Treemap, ace_Sunburst, ace_Sankey, ace_Heatmap, ace_DataGrid, ace_Table, ace_Tree, ace_TextEdit });
            accordionControl1.Location = new Point(0, 41);
            accordionControl1.Name = "accordionControl1";
            accordionControl1.ScrollBarMode = DevExpress.XtraBars.Navigation.ScrollBarMode.Touch;
            accordionControl1.Size = new Size(226, 635);
            accordionControl1.TabIndex = 1;
            accordionControl1.ViewType = DevExpress.XtraBars.Navigation.AccordionControlViewType.HamburgerMenu;
            // 
            // ace_Treemap
            // 
            ace_Treemap.Expanded = true;
            ace_Treemap.ImageOptions.SvgImage = (DevExpress.Utils.Svg.SvgImage)resources.GetObject("ace_Treemap.ImageOptions.SvgImage");
            ace_Treemap.Name = "ace_Treemap";
            ace_Treemap.Style = DevExpress.XtraBars.Navigation.ElementStyle.Item;
            ace_Treemap.Text = "공정 상태";
            // 
            // ace_Sunburst
            // 
            ace_Sunburst.ImageOptions.SvgImage = (DevExpress.Utils.Svg.SvgImage)resources.GetObject("ace_Sunburst.ImageOptions.SvgImage");
            ace_Sunburst.Name = "ace_Sunburst";
            ace_Sunburst.Style = DevExpress.XtraBars.Navigation.ElementStyle.Item;
            ace_Sunburst.Text = "이상 보기";
            // 
            // ace_Sankey
            // 
            ace_Sankey.ImageOptions.SvgImage = (DevExpress.Utils.Svg.SvgImage)resources.GetObject("ace_Sankey.ImageOptions.SvgImage");
            ace_Sankey.Name = "ace_Sankey";
            ace_Sankey.Style = DevExpress.XtraBars.Navigation.ElementStyle.Item;
            ace_Sankey.Text = "동작 순서";
            // 
            // ace_Heatmap
            // 
            ace_Heatmap.ImageOptions.SvgImage = (DevExpress.Utils.Svg.SvgImage)resources.GetObject("ace_Heatmap.ImageOptions.SvgImage");
            ace_Heatmap.Name = "ace_Heatmap";
            ace_Heatmap.Style = DevExpress.XtraBars.Navigation.ElementStyle.Item;
            ace_Heatmap.Text = "편차 분석";
            // 
            // ace_DataGrid
            // 
            ace_DataGrid.ImageOptions.SvgImage = (DevExpress.Utils.Svg.SvgImage)resources.GetObject("ace_DataGrid.ImageOptions.SvgImage");
            ace_DataGrid.Name = "ace_DataGrid";
            ace_DataGrid.Style = DevExpress.XtraBars.Navigation.ElementStyle.Item;
            ace_DataGrid.Text = "IO 모니터링";
            // 
            // ace_Table
            // 
            ace_Table.ImageOptions.SvgImage = (DevExpress.Utils.Svg.SvgImage)resources.GetObject("ace_Table.ImageOptions.SvgImage");
            ace_Table.Name = "ace_Table";
            ace_Table.Style = DevExpress.XtraBars.Navigation.ElementStyle.Item;
            ace_Table.Text = "TAG 모니터링";
            // 
            // ace_Tree
            // 
            ace_Tree.ImageOptions.SvgImage = (DevExpress.Utils.Svg.SvgImage)resources.GetObject("ace_Tree.ImageOptions.SvgImage");
            ace_Tree.Name = "ace_Tree";
            ace_Tree.Style = DevExpress.XtraBars.Navigation.ElementStyle.Item;
            ace_Tree.Text = "모델 구조";
            // 
            // ace_TextEdit
            // 
            ace_TextEdit.ImageOptions.SvgImage = (DevExpress.Utils.Svg.SvgImage)resources.GetObject("ace_TextEdit.ImageOptions.SvgImage");
            ace_TextEdit.Name = "ace_TextEdit";
            ace_TextEdit.Style = DevExpress.XtraBars.Navigation.ElementStyle.Item;
            ace_TextEdit.Text = "모델 텍스트";
            // 
            // fluentDesignFormControl1
            // 
            fluentDesignFormControl1.FluentDesignForm = this;
            fluentDesignFormControl1.Items.AddRange(new DevExpress.XtraBars.BarItem[] { skinDropDownButtonItem1, skinPaletteDropDownButtonItem1 });
            fluentDesignFormControl1.Location = new Point(0, 0);
            fluentDesignFormControl1.Manager = fluentFormDefaultManager1;
            fluentDesignFormControl1.Name = "fluentDesignFormControl1";
            fluentDesignFormControl1.Size = new Size(1475, 41);
            fluentDesignFormControl1.TabIndex = 2;
            fluentDesignFormControl1.TabStop = false;
            fluentDesignFormControl1.TitleItemLinks.Add(skinPaletteDropDownButtonItem1);
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
            // fluentFormDefaultManager1
            // 
            fluentFormDefaultManager1.Form = this;
            fluentFormDefaultManager1.Items.AddRange(new DevExpress.XtraBars.BarItem[] { skinDropDownButtonItem1, skinPaletteDropDownButtonItem1 });
            fluentFormDefaultManager1.MaxItemId = 2;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 14F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1475, 676);
            ControlContainer = fluentDesignFormContainer1;
            Controls.Add(fluentDesignFormContainer1);
            Controls.Add(accordionControl1);
            Controls.Add(fluentDesignFormControl1);
            FluentDesignFormControl = fluentDesignFormControl1;
            IconOptions.Icon = (Icon)resources.GetObject("MainForm.IconOptions.Icon");
            Name = "MainForm";
            NavigationControl = accordionControl1;
            Text = "DS Pilot";
            fluentDesignFormContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)navigationFrame1).EndInit();
            navigationFrame1.ResumeLayout(false);
            navigationPage7.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)accordionControl1).EndInit();
            ((System.ComponentModel.ISupportInitialize)fluentDesignFormControl1).EndInit();
            ((System.ComponentModel.ISupportInitialize)fluentFormDefaultManager1).EndInit();
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
        private DevExpress.XtraBars.Navigation.NavigationFrame navigationFrame1;
        private DevExpress.XtraBars.Navigation.NavigationPage navigationPage1;
        private DevExpress.XtraBars.Navigation.NavigationPage navigationPage2;
        private DevExpress.XtraBars.Navigation.NavigationPage navigationPage3;
        private DevExpress.XtraBars.Navigation.NavigationPage navigationPage4;
        private DevExpress.XtraBars.Navigation.NavigationPage navigationPage5;
        private DevExpress.XtraBars.Navigation.NavigationPage navigationPage6;
        private DevExpress.XtraBars.Navigation.NavigationPage navigationPage7;
        private DevExpress.XtraBars.Navigation.AccordionControlElement ace_DataGrid;
        private UserControl.UcDsDataGrid ucDsDataGrid1;
        private UserControl.UcDsTextEdit ucDsTextEdit1;
        private DevExpress.XtraBars.SkinDropDownButtonItem skinDropDownButtonItem1;
        private DevExpress.XtraBars.SkinPaletteDropDownButtonItem skinPaletteDropDownButtonItem1;
        private DevExpress.XtraBars.Navigation.NavigationPage navigationPage8;
        private DevExpress.XtraBars.Navigation.AccordionControlElement ace_TextEdit;
    }
}