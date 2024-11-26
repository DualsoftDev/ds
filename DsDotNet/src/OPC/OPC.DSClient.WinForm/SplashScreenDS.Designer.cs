
namespace OPC.DSClient.WinForm
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
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SplashScreenDS));
            peImage = new DevExpress.XtraEditors.PictureEdit();
            labelControl2 = new DevExpress.XtraEditors.LabelControl();
            labelControl_ReferencedAssemblies = new DevExpress.XtraEditors.LabelControl();
            labelControl1 = new DevExpress.XtraEditors.LabelControl();
            labelCopyright = new DevExpress.XtraEditors.LabelControl();
            labelControl_Ver = new DevExpress.XtraEditors.LabelControl();
            timer1 = new System.Windows.Forms.Timer(components);
            labelControl_Process = new DevExpress.XtraEditors.LabelControl();
            ((System.ComponentModel.ISupportInitialize)peImage.Properties).BeginInit();
            SuspendLayout();
            // 
            // peImage
            // 
            peImage.Dock = DockStyle.Fill;
            peImage.EditValue = resources.GetObject("peImage.EditValue");
            peImage.Location = new Point(1, 1);
            peImage.Margin = new Padding(3, 4, 3, 4);
            peImage.Name = "peImage";
            peImage.Properties.AllowFocused = false;
            peImage.Properties.Appearance.BackColor = Color.Transparent;
            peImage.Properties.Appearance.Options.UseBackColor = true;
            peImage.Properties.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            peImage.Properties.ShowMenu = false;
            peImage.Properties.SizeMode = DevExpress.XtraEditors.Controls.PictureSizeMode.Stretch;
            peImage.Properties.SvgImageColorizationMode = DevExpress.Utils.SvgImageColorizationMode.None;
            peImage.Size = new Size(502, 313);
            peImage.TabIndex = 9;
            // 
            // labelControl2
            // 
            labelControl2.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            labelControl2.Appearance.BackColor = Color.White;
            labelControl2.Appearance.ForeColor = Color.Black;
            labelControl2.Appearance.Options.UseBackColor = true;
            labelControl2.Appearance.Options.UseFont = true;
            labelControl2.Appearance.Options.UseForeColor = true;
            labelControl2.Location = new Point(24, 232);
            labelControl2.Margin = new Padding(3, 4, 3, 4);
            labelControl2.Name = "labelControl2";
            labelControl2.Size = new Size(55, 14);
            labelControl2.TabIndex = 29;
            labelControl2.Text = "Starting...";
            // 
            // labelControl_ReferencedAssemblies
            // 
            labelControl_ReferencedAssemblies.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            labelControl_ReferencedAssemblies.Appearance.BackColor = Color.White;
            labelControl_ReferencedAssemblies.Appearance.ForeColor = Color.Black;
            labelControl_ReferencedAssemblies.Appearance.Options.UseBackColor = true;
            labelControl_ReferencedAssemblies.Appearance.Options.UseFont = true;
            labelControl_ReferencedAssemblies.Appearance.Options.UseForeColor = true;
            labelControl_ReferencedAssemblies.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            labelControl_ReferencedAssemblies.Location = new Point(98, 232);
            labelControl_ReferencedAssemblies.Margin = new Padding(3, 4, 3, 4);
            labelControl_ReferencedAssemblies.Name = "labelControl_ReferencedAssemblies";
            labelControl_ReferencedAssemblies.Size = new Size(121, 14);
            labelControl_ReferencedAssemblies.TabIndex = 28;
            labelControl_ReferencedAssemblies.Text = "ReferencedAssemblies";
            // 
            // labelControl1
            // 
            labelControl1.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            labelControl1.Appearance.BackColor = Color.White;
            labelControl1.Appearance.ForeColor = Color.Black;
            labelControl1.Appearance.Options.UseBackColor = true;
            labelControl1.Appearance.Options.UseFont = true;
            labelControl1.Appearance.Options.UseForeColor = true;
            labelControl1.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            labelControl1.Location = new Point(353, 274);
            labelControl1.Margin = new Padding(3, 4, 3, 4);
            labelControl1.Name = "labelControl1";
            labelControl1.Size = new Size(42, 14);
            labelControl1.TabIndex = 26;
            labelControl1.Text = "DS Pilot";
            // 
            // labelCopyright
            // 
            labelCopyright.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            labelCopyright.Appearance.BackColor = Color.White;
            labelCopyright.Appearance.ForeColor = Color.Black;
            labelCopyright.Appearance.Options.UseBackColor = true;
            labelCopyright.Appearance.Options.UseFont = true;
            labelCopyright.Appearance.Options.UseForeColor = true;
            labelCopyright.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            labelCopyright.Location = new Point(24, 274);
            labelCopyright.Margin = new Padding(3, 4, 3, 4);
            labelCopyright.Name = "labelCopyright";
            labelCopyright.Size = new Size(247, 14);
            labelCopyright.TabIndex = 27;
            labelCopyright.Text = "Copyright © 2019 Dualsoft All right reserved.";
            // 
            // labelControl_Ver
            // 
            labelControl_Ver.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            labelControl_Ver.Appearance.BackColor = Color.White;
            labelControl_Ver.Appearance.ForeColor = Color.Black;
            labelControl_Ver.Appearance.Options.UseBackColor = true;
            labelControl_Ver.Appearance.Options.UseFont = true;
            labelControl_Ver.Appearance.Options.UseForeColor = true;
            labelControl_Ver.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            labelControl_Ver.Location = new Point(24, 24);
            labelControl_Ver.Margin = new Padding(3, 4, 3, 4);
            labelControl_Ver.Name = "labelControl_Ver";
            labelControl_Ver.Size = new Size(19, 14);
            labelControl_Ver.TabIndex = 25;
            labelControl_Ver.Text = "Ver";
            // 
            // labelControl_Process
            // 
            labelControl_Process.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            labelControl_Process.Appearance.BackColor = Color.White;
            labelControl_Process.Appearance.ForeColor = Color.Black;
            labelControl_Process.Appearance.Options.UseBackColor = true;
            labelControl_Process.Appearance.Options.UseFont = true;
            labelControl_Process.Appearance.Options.UseForeColor = true;
            labelControl_Process.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            labelControl_Process.Location = new Point(24, 254);
            labelControl_Process.Margin = new Padding(3, 4, 3, 4);
            labelControl_Process.Name = "labelControl_Process";
            labelControl_Process.Size = new Size(0, 14);
            labelControl_Process.TabIndex = 27;
            // 
            // SplashScreenDS
            // 
            AutoScaleDimensions = new SizeF(7F, 14F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(504, 315);
            Controls.Add(labelControl2);
            Controls.Add(labelControl_ReferencedAssemblies);
            Controls.Add(labelControl1);
            Controls.Add(labelControl_Process);
            Controls.Add(labelCopyright);
            Controls.Add(labelControl_Ver);
            Controls.Add(peImage);
            Margin = new Padding(3, 4, 3, 4);
            Name = "SplashScreenDS";
            Padding = new Padding(1);
            Text = "SplashScreen1";
            ((System.ComponentModel.ISupportInitialize)peImage.Properties).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private DevExpress.XtraEditors.PictureEdit peImage;
        private DevExpress.XtraEditors.LabelControl labelControl2;
        private DevExpress.XtraEditors.LabelControl labelControl_ReferencedAssemblies;
        private DevExpress.XtraEditors.LabelControl labelControl1;
        private DevExpress.XtraEditors.LabelControl labelCopyright;
        private DevExpress.XtraEditors.LabelControl labelControl_Ver;
        private System.Windows.Forms.Timer timer1;
        private DevExpress.XtraEditors.LabelControl labelControl_Process;
    }
}
