using Opc.Ua.Client.Controls;

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
            ucDsTreemap1 = new UserControl.UcDsTreemap();
            ucDsTree1 = new UserControl.UcDsTree();
            ucDsTable1 = new UserControl.UcDsTable();
            ucDsSunburst1 = new UserControl.UcDsSunburst();
            ucDsSankey1 = new UserControl.UcDsSankey();
            ucDsHeatmap1 = new UserControl.UcDsHeatmap();
            accordionControl1 = new DevExpress.XtraBars.Navigation.AccordionControl();
            ace_Table = new DevExpress.XtraBars.Navigation.AccordionControlElement();
            ace_Tree = new DevExpress.XtraBars.Navigation.AccordionControlElement();
            ace_Heatmap = new DevExpress.XtraBars.Navigation.AccordionControlElement();
            ace_Treemap = new DevExpress.XtraBars.Navigation.AccordionControlElement();
            ace_Sunburst = new DevExpress.XtraBars.Navigation.AccordionControlElement();
            ace_Sankey = new DevExpress.XtraBars.Navigation.AccordionControlElement();
            fluentDesignFormControl1 = new DevExpress.XtraBars.FluentDesignSystem.FluentDesignFormControl();
            fluentFormDefaultManager1 = new DevExpress.XtraBars.FluentDesignSystem.FluentFormDefaultManager(components);
            fluentDesignFormContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)accordionControl1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)fluentDesignFormControl1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)fluentFormDefaultManager1).BeginInit();
            SuspendLayout();
            // 
            // fluentDesignFormContainer1
            // 
            fluentDesignFormContainer1.Controls.Add(ucDsSankey1);
            fluentDesignFormContainer1.Controls.Add(ucDsTreemap1);
            fluentDesignFormContainer1.Controls.Add(ucDsSunburst1);
            fluentDesignFormContainer1.Controls.Add(ucDsTree1);
            fluentDesignFormContainer1.Controls.Add(ucDsTable1);
            fluentDesignFormContainer1.Controls.Add(ucDsHeatmap1);
            fluentDesignFormContainer1.Dock = DockStyle.Fill;
            fluentDesignFormContainer1.Location = new Point(226, 34);
            fluentDesignFormContainer1.Name = "fluentDesignFormContainer1";
            fluentDesignFormContainer1.Size = new Size(1245, 736);
            fluentDesignFormContainer1.TabIndex = 0;
            // 
            // ucDsTreemap1
            // 
            ucDsTreemap1.Dock = DockStyle.Fill;
            ucDsTreemap1.Location = new Point(0, 0);
            ucDsTreemap1.Name = "ucDsTreemap1";
            ucDsTreemap1.Size = new Size(1245, 736);
            ucDsTreemap1.TabIndex = 5;
            // 
            // ucDsTree1
            // 
            ucDsTree1.Dock = DockStyle.Fill;
            ucDsTree1.Location = new Point(0, 0);
            ucDsTree1.Name = "ucDsTree1";
            ucDsTree1.Size = new Size(150, 150);
            ucDsTree1.TabIndex = 6;
            // 
            // ucDsTable1
            // 
            ucDsTable1.Dock = DockStyle.Fill;
            ucDsTable1.Location = new Point(0, 0);
            ucDsTable1.Name = "ucDsTable1";
            ucDsTable1.Size = new Size(1245, 736);
            ucDsTable1.TabIndex = 3;
            // 
            // ucDsSunburst1
            // 
            ucDsSunburst1.Dock = DockStyle.Fill;
            ucDsSunburst1.Location = new Point(0, 0);
            ucDsSunburst1.Name = "ucDsSunburst1";
            ucDsSunburst1.Size = new Size(1245, 736);
            ucDsSunburst1.TabIndex = 2;
            // 
            // ucDsSankey1
            // 
            ucDsSankey1.Dock = DockStyle.Fill;
            ucDsSankey1.Location = new Point(0, 0);
            ucDsSankey1.Name = "ucDsSankey1";
            ucDsSankey1.Size = new Size(1245, 736);
            ucDsSankey1.TabIndex = 1;
            // 
            // ucDsHeatmap1
            // 
            ucDsHeatmap1.Dock = DockStyle.Fill;
            ucDsHeatmap1.Location = new Point(0, 0);
            ucDsHeatmap1.Name = "ucDsHeatmap1";
            ucDsHeatmap1.Size = new Size(1245, 736);
            ucDsHeatmap1.TabIndex = 0;
            // 
            // accordionControl1
            // 
            accordionControl1.Dock = DockStyle.Left;
            accordionControl1.Elements.AddRange(new DevExpress.XtraBars.Navigation.AccordionControlElement[] { ace_Table, ace_Tree, ace_Heatmap, ace_Treemap, ace_Sunburst, ace_Sankey });
            accordionControl1.Location = new Point(0, 34);
            accordionControl1.Name = "accordionControl1";
            accordionControl1.ScrollBarMode = DevExpress.XtraBars.Navigation.ScrollBarMode.Touch;
            accordionControl1.Size = new Size(226, 736);
            accordionControl1.TabIndex = 1;
            accordionControl1.ViewType = DevExpress.XtraBars.Navigation.AccordionControlViewType.HamburgerMenu;
            // 
            // ace_Table
            // 
            ace_Table.ImageOptions.SvgImage = (DevExpress.Utils.Svg.SvgImage)resources.GetObject("ace_Table.ImageOptions.SvgImage");
            ace_Table.Name = "ace_Table";
            ace_Table.Style = DevExpress.XtraBars.Navigation.ElementStyle.Item;
            ace_Table.Text = "Table";
            // 
            // ace_Tree
            // 
            ace_Tree.ImageOptions.SvgImage = (DevExpress.Utils.Svg.SvgImage)resources.GetObject("ace_Tree.ImageOptions.SvgImage");
            ace_Tree.Name = "ace_Tree";
            ace_Tree.Style = DevExpress.XtraBars.Navigation.ElementStyle.Item;
            ace_Tree.Text = "Tree";
            // 
            // ace_Heatmap
            // 
            ace_Heatmap.ImageOptions.SvgImage = (DevExpress.Utils.Svg.SvgImage)resources.GetObject("ace_Heatmap.ImageOptions.SvgImage");
            ace_Heatmap.Name = "ace_Heatmap";
            ace_Heatmap.Style = DevExpress.XtraBars.Navigation.ElementStyle.Item;
            ace_Heatmap.Text = "Heatmap";
            // 
            // ace_Treemap
            // 
            ace_Treemap.Expanded = true;
            ace_Treemap.ImageOptions.SvgImage = (DevExpress.Utils.Svg.SvgImage)resources.GetObject("ace_Treemap.ImageOptions.SvgImage");
            ace_Treemap.Name = "ace_Treemap";
            ace_Treemap.Style = DevExpress.XtraBars.Navigation.ElementStyle.Item;
            ace_Treemap.Text = "Treemap";
            // 
            // ace_Sunburst
            // 
            ace_Sunburst.ImageOptions.SvgImage = (DevExpress.Utils.Svg.SvgImage)resources.GetObject("ace_Sunburst.ImageOptions.SvgImage");
            ace_Sunburst.Name = "ace_Sunburst";
            ace_Sunburst.Style = DevExpress.XtraBars.Navigation.ElementStyle.Item;
            ace_Sunburst.Text = "Sunburst";
            // 
            // ace_Sankey
            // 
            ace_Sankey.ImageOptions.SvgImage = (DevExpress.Utils.Svg.SvgImage)resources.GetObject("ace_Sankey.ImageOptions.SvgImage");
            ace_Sankey.Name = "ace_Sankey";
            ace_Sankey.Style = DevExpress.XtraBars.Navigation.ElementStyle.Item;
            ace_Sankey.Text = "Sankey";
            // 
            // fluentDesignFormControl1
            // 
            fluentDesignFormControl1.FluentDesignForm = this;
            fluentDesignFormControl1.Location = new Point(0, 0);
            fluentDesignFormControl1.Manager = fluentFormDefaultManager1;
            fluentDesignFormControl1.Name = "fluentDesignFormControl1";
            fluentDesignFormControl1.Size = new Size(1471, 34);
            fluentDesignFormControl1.TabIndex = 2;
            fluentDesignFormControl1.TabStop = false;
            // 
            // fluentFormDefaultManager1
            // 
            fluentFormDefaultManager1.Form = this;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 16F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1471, 770);
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
    }
}