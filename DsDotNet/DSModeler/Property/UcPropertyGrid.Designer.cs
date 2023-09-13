
namespace DSModeler
{
    partial class UcPropertyGrid
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
            this.PropertyGrid = new DevExpress.XtraVerticalGrid.PropertyGridControl();
            ((System.ComponentModel.ISupportInitialize)(this.PropertyGrid)).BeginInit();
            this.SuspendLayout();
            // 
            // propertyGridControl1
            // 
            this.PropertyGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.PropertyGrid.Location = new System.Drawing.Point(0, 0);
            this.PropertyGrid.Name = "propertyGridControl1";
            this.PropertyGrid.Size = new System.Drawing.Size(916, 601);
            this.PropertyGrid.TabIndex = 0;
            // 
            // UcPropertyGrid
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.PropertyGrid);
            this.Name = "UcPropertyGrid";
            this.Size = new System.Drawing.Size(916, 601);
            ((System.ComponentModel.ISupportInitialize)(this.PropertyGrid)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

    }
}
