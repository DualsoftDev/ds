
namespace Dualsoft.Utils
{
    partial class SplashScreenDS
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SplashScreenDS));
            this.peImage = new DevExpress.XtraEditors.PictureEdit();
            this.labelControl2 = new DevExpress.XtraEditors.LabelControl();
            this.labelControl_ReferencedAssemblies = new DevExpress.XtraEditors.LabelControl();
            this.labelControl1 = new DevExpress.XtraEditors.LabelControl();
            this.labelCopyright = new DevExpress.XtraEditors.LabelControl();
            this.labelControl_Ver = new DevExpress.XtraEditors.LabelControl();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.peImage.Properties)).BeginInit();
            this.SuspendLayout();
            // 
            // peImage
            // 
            this.peImage.Dock = System.Windows.Forms.DockStyle.Fill;
            this.peImage.EditValue = ((object)(resources.GetObject("peImage.EditValue")));
            this.peImage.Location = new System.Drawing.Point(1, 1);
            this.peImage.Name = "peImage";
            this.peImage.Properties.AllowFocused = false;
            this.peImage.Properties.Appearance.BackColor = System.Drawing.Color.Transparent;
            this.peImage.Properties.Appearance.Options.UseBackColor = true;
            this.peImage.Properties.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.peImage.Properties.ShowMenu = false;
            this.peImage.Properties.SizeMode = DevExpress.XtraEditors.Controls.PictureSizeMode.Stretch;
            this.peImage.Properties.SvgImageColorizationMode = DevExpress.Utils.SvgImageColorizationMode.None;
            this.peImage.Size = new System.Drawing.Size(502, 268);
            this.peImage.TabIndex = 9;
            // 
            // labelControl2
            // 
            this.labelControl2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelControl2.Appearance.BackColor = System.Drawing.Color.White;
            this.labelControl2.Appearance.ForeColor = System.Drawing.Color.Black;
            this.labelControl2.Appearance.Options.UseBackColor = true;
            this.labelControl2.Appearance.Options.UseFont = true;
            this.labelControl2.Appearance.Options.UseForeColor = true;
            this.labelControl2.Location = new System.Drawing.Point(24, 199);
            this.labelControl2.Name = "labelControl2";
            this.labelControl2.Size = new System.Drawing.Size(55, 14);
            this.labelControl2.TabIndex = 29;
            this.labelControl2.Text = "Starting...";
            // 
            // labelControl_ReferencedAssemblies
            // 
            this.labelControl_ReferencedAssemblies.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelControl_ReferencedAssemblies.Appearance.BackColor = System.Drawing.Color.White;
            this.labelControl_ReferencedAssemblies.Appearance.ForeColor = System.Drawing.Color.Black;
            this.labelControl_ReferencedAssemblies.Appearance.Options.UseBackColor = true;
            this.labelControl_ReferencedAssemblies.Appearance.Options.UseFont = true;
            this.labelControl_ReferencedAssemblies.Appearance.Options.UseForeColor = true;
            this.labelControl_ReferencedAssemblies.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.labelControl_ReferencedAssemblies.Location = new System.Drawing.Point(98, 199);
            this.labelControl_ReferencedAssemblies.Name = "labelControl_ReferencedAssemblies";
            this.labelControl_ReferencedAssemblies.Size = new System.Drawing.Size(121, 14);
            this.labelControl_ReferencedAssemblies.TabIndex = 28;
            this.labelControl_ReferencedAssemblies.Text = "ReferencedAssemblies";
            // 
            // labelControl1
            // 
            this.labelControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelControl1.Appearance.BackColor = System.Drawing.Color.White;
            this.labelControl1.Appearance.ForeColor = System.Drawing.Color.Black;
            this.labelControl1.Appearance.Options.UseBackColor = true;
            this.labelControl1.Appearance.Options.UseFont = true;
            this.labelControl1.Appearance.Options.UseForeColor = true;
            this.labelControl1.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.labelControl1.Location = new System.Drawing.Point(353, 235);
            this.labelControl1.Name = "labelControl1";
            this.labelControl1.Size = new System.Drawing.Size(91, 14);
            this.labelControl1.TabIndex = 26;
            this.labelControl1.Text = "System Designer";
            // 
            // labelCopyright
            // 
            this.labelCopyright.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelCopyright.Appearance.BackColor = System.Drawing.Color.White;
            this.labelCopyright.Appearance.ForeColor = System.Drawing.Color.Black;
            this.labelCopyright.Appearance.Options.UseBackColor = true;
            this.labelCopyright.Appearance.Options.UseFont = true;
            this.labelCopyright.Appearance.Options.UseForeColor = true;
            this.labelCopyright.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.labelCopyright.Location = new System.Drawing.Point(24, 235);
            this.labelCopyright.Name = "labelCopyright";
            this.labelCopyright.Size = new System.Drawing.Size(249, 14);
            this.labelCopyright.TabIndex = 27;
            this.labelCopyright.Text = "Copyright © 2019 Dual inc. All right reserved.";
            // 
            // labelControl_Ver
            // 
            this.labelControl_Ver.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.labelControl_Ver.Appearance.BackColor = System.Drawing.Color.White;
            this.labelControl_Ver.Appearance.ForeColor = System.Drawing.Color.Black;
            this.labelControl_Ver.Appearance.Options.UseBackColor = true;
            this.labelControl_Ver.Appearance.Options.UseFont = true;
            this.labelControl_Ver.Appearance.Options.UseForeColor = true;
            this.labelControl_Ver.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.labelControl_Ver.Location = new System.Drawing.Point(24, 21);
            this.labelControl_Ver.Name = "labelControl_Ver";
            this.labelControl_Ver.Size = new System.Drawing.Size(19, 14);
            this.labelControl_Ver.TabIndex = 25;
            this.labelControl_Ver.Text = "Ver";
            // 
            // SplashScreenDS
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(504, 270);
            this.Controls.Add(this.labelControl2);
            this.Controls.Add(this.labelControl_ReferencedAssemblies);
            this.Controls.Add(this.labelControl1);
            this.Controls.Add(this.labelCopyright);
            this.Controls.Add(this.labelControl_Ver);
            this.Controls.Add(this.peImage);
            this.Name = "SplashScreenDS";
            this.Padding = new System.Windows.Forms.Padding(1);
            this.Text = "SplashScreen1";
            ((System.ComponentModel.ISupportInitialize)(this.peImage.Properties)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private DevExpress.XtraEditors.PictureEdit peImage;
        private DevExpress.XtraEditors.LabelControl labelControl2;
        private DevExpress.XtraEditors.LabelControl labelControl_ReferencedAssemblies;
        private DevExpress.XtraEditors.LabelControl labelControl1;
        private DevExpress.XtraEditors.LabelControl labelCopyright;
        private DevExpress.XtraEditors.LabelControl labelControl_Ver;
        private System.Windows.Forms.Timer timer1;
    }
}
