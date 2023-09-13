namespace DSModeler.Form
{
    partial class FormDocImage
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormDocImage));
            this.ImageControl = new DevExpress.XtraEditors.PictureEdit();
            ((System.ComponentModel.ISupportInitialize)(this.ImageControl.Properties)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureEdit1
            // 
            this.ImageControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ImageControl.EditValue = ((object)(resources.GetObject("pictureEdit1.EditValue")));
            this.ImageControl.Location = new System.Drawing.Point(0, 0);
            this.ImageControl.Name = "pictureEdit1";
            this.ImageControl.Properties.ShowCameraMenuItem = DevExpress.XtraEditors.Controls.CameraMenuItemVisibility.Auto;
            this.ImageControl.Properties.ShowMenu = false;
            this.ImageControl.Properties.SizeMode = DevExpress.XtraEditors.Controls.PictureSizeMode.Zoom;
            this.ImageControl.Size = new System.Drawing.Size(694, 665);
            this.ImageControl.TabIndex = 0;
            // 
            // FormDocViewStart
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(694, 665);
            this.Controls.Add(this.ImageControl);
            this.Name = "FormDocViewStart";
            this.Text = "Start";
            ((System.ComponentModel.ISupportInitialize)(this.ImageControl.Properties)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

    }
}