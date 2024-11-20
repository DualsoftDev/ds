using System.Drawing;
using System.Windows.Forms;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;

namespace OPC.UA.DSClient.Winform
{
    partial class TagViewerForm
    {
        private System.ComponentModel.IContainer components = null;
        private GridControl gridControl;
        private GridView gridView;

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

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.gridControl = new GridControl();
            this.gridView = new GridView();

            ((System.ComponentModel.ISupportInitialize)(this.gridControl)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridView)).BeginInit();
            this.SuspendLayout();

            // 
            // gridControl
            // 
            this.gridControl.Dock = DockStyle.Fill;
            this.gridControl.Location = new Point(0, 0);
            this.gridControl.MainView = this.gridView;
            this.gridControl.Name = "gridControl";
            this.gridControl.Size = new Size(1031, 630);
            this.gridControl.TabIndex = 0;
            this.gridControl.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.gridView});

            // 
            // gridView
            // 
            this.gridView.GridControl = this.gridControl;
            this.gridView.Name = "gridView";
            this.gridView.OptionsView.ShowAutoFilterRow = true; // Enable filter row
            this.gridView.OptionsBehavior.Editable = false; // Read-only grid

            // 
            // TagViewerForm
            // 
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(1031, 630);
            this.Controls.Add(this.gridControl);
            this.Margin = new Padding(4);
            this.Name = "TagViewerForm";
            this.Text = "Tag Viewer";

            ((System.ComponentModel.ISupportInitialize)(this.gridControl)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridView)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion
    }
}
