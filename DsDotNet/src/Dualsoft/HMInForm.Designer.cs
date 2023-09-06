namespace DSModeler
{
    partial class HMIForm
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(HMIForm));
            this.documentManager1 = new DevExpress.XtraBars.Docking2010.DocumentManager(this.components);
            this.windowsUIView = new DevExpress.XtraBars.Docking2010.Views.WindowsUI.WindowsUIView(this.components);
            this.tileContainerDS = new DevExpress.XtraBars.Docking2010.Views.WindowsUI.TileContainer(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.documentManager1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.windowsUIView)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tileContainerDS)).BeginInit();
            this.SuspendLayout();
            // 
            // documentManager1
            // 
            this.documentManager1.ContainerControl = this;
            this.documentManager1.View = this.windowsUIView;
            this.documentManager1.ViewCollection.AddRange(new DevExpress.XtraBars.Docking2010.Views.BaseView[] {
            this.windowsUIView});
            // 
            // windowsUIView
            // 
            this.windowsUIView.Appearance.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(33)))), ((int)(((byte)(33)))));
            this.windowsUIView.Appearance.Options.UseBackColor = true;
            this.windowsUIView.AppearanceCaption.Font = new System.Drawing.Font("Segoe UI Light", 36F);
            this.windowsUIView.AppearanceCaption.Options.UseFont = true;
            this.windowsUIView.Caption = "Header";
            this.windowsUIView.ContentContainers.AddRange(new DevExpress.XtraBars.Docking2010.Views.WindowsUI.IContentContainer[] {
            this.tileContainerDS});
            // 
            // tileContainerDS
            // 
            this.tileContainerDS.Name = "tileContainer1";
            this.tileContainerDS.Properties.Orientation = System.Windows.Forms.Orientation.Vertical;
            this.tileContainerDS.Properties.Padding = new System.Windows.Forms.Padding(30, 0, 30, 0);
            this.tileContainerDS.Properties.ShowGroupText = DevExpress.Utils.DefaultBoolean.True;
            // 
            // HMIForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1218, 623);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.IconOptions.SvgImage = ((DevExpress.Utils.Svg.SvgImage)(resources.GetObject("HMIForm.IconOptions.SvgImage")));
            this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.Name = "HMIForm";
            this.Text = "Form1";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Load += new System.EventHandler(this.HMIForm_Load);
            this.Shown += new System.EventHandler(this.HMIForm_Shown);
            ((System.ComponentModel.ISupportInitialize)(this.documentManager1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.windowsUIView)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tileContainerDS)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private DevExpress.XtraBars.Docking2010.DocumentManager documentManager1;
        private DevExpress.XtraBars.Docking2010.Views.WindowsUI.WindowsUIView windowsUIView;
        private DevExpress.XtraBars.Docking2010.Views.WindowsUI.TileContainer tileContainerDS;
    }
}

