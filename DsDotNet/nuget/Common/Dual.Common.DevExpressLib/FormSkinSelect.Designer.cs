namespace Dual.Common.DevExpressLib
{
    partial class FormSkinSelect
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormSkinSelect));
            this.behaviorManager1 = new DevExpress.Utils.Behaviors.BehaviorManager(this.components);
            this.simpleButton_OK = new DevExpress.XtraEditors.SimpleButton();
            this.labelControl1 = new DevExpress.XtraEditors.LabelControl();
            this.labelControl2 = new DevExpress.XtraEditors.LabelControl();
            this.pictureEdit2 = new DevExpress.XtraEditors.PictureEdit();
            this.pictureEdit1 = new DevExpress.XtraEditors.PictureEdit();
            this.radioButton_White = new System.Windows.Forms.RadioButton();
            this.radioButton_Black = new System.Windows.Forms.RadioButton();
            this.checkEdit_Showing = new DevExpress.XtraEditors.CheckEdit();
            ((System.ComponentModel.ISupportInitialize)(this.behaviorManager1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureEdit2.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureEdit1.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.checkEdit_Showing.Properties)).BeginInit();
            this.SuspendLayout();
            //
            // simpleButton_OK
            //
            this.simpleButton_OK.Appearance.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.simpleButton_OK.Appearance.Options.UseFont = true;
            this.simpleButton_OK.ImageOptions.SvgImage = ((DevExpress.Utils.Svg.SvgImage)(resources.GetObject("simpleButton_OK.ImageOptions.SvgImage")));
            this.simpleButton_OK.Location = new System.Drawing.Point(89, 171);
            this.simpleButton_OK.Name = "simpleButton_OK";
            this.simpleButton_OK.Size = new System.Drawing.Size(121, 35);
            this.simpleButton_OK.TabIndex = 1;
            this.simpleButton_OK.Text = "확인";
            this.simpleButton_OK.Click += new System.EventHandler(this.simpleButton_OK_Click);
            //
            // labelControl1
            //
            this.labelControl1.Appearance.Font = new System.Drawing.Font("Tahoma", 12F);
            this.labelControl1.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.labelControl1.Appearance.Options.UseFont = true;
            this.labelControl1.Appearance.Options.UseForeColor = true;
            this.labelControl1.Location = new System.Drawing.Point(194, 20);
            this.labelControl1.Name = "labelControl1";
            this.labelControl1.Size = new System.Drawing.Size(0, 19);
            this.labelControl1.TabIndex = 4;
            //
            // labelControl2
            //
            this.labelControl2.Appearance.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelControl2.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.labelControl2.Appearance.Options.UseFont = true;
            this.labelControl2.Appearance.Options.UseForeColor = true;
            this.labelControl2.Location = new System.Drawing.Point(53, 20);
            this.labelControl2.Name = "labelControl2";
            this.labelControl2.Size = new System.Drawing.Size(0, 19);
            this.labelControl2.TabIndex = 5;
            //
            // pictureEdit2
            //
            this.pictureEdit2.EditValue = global::Dual.Common.DevExpressLib.Properties.Resources.black;
            this.pictureEdit2.Location = new System.Drawing.Point(156, 45);
            this.pictureEdit2.Name = "pictureEdit2";
            this.pictureEdit2.Properties.ShowCameraMenuItem = DevExpress.XtraEditors.Controls.CameraMenuItemVisibility.Auto;
            this.pictureEdit2.Properties.SizeMode = DevExpress.XtraEditors.Controls.PictureSizeMode.Stretch;
            this.pictureEdit2.Properties.SvgImageSize = new System.Drawing.Size(80, 80);
            this.pictureEdit2.Size = new System.Drawing.Size(121, 117);
            this.pictureEdit2.TabIndex = 7;
            //
            // pictureEdit1
            //
            this.pictureEdit1.EditValue = global::Dual.Common.DevExpressLib.Properties.Resources.white;
            this.pictureEdit1.Location = new System.Drawing.Point(29, 45);
            this.pictureEdit1.Name = "pictureEdit1";
            this.pictureEdit1.Properties.ShowCameraMenuItem = DevExpress.XtraEditors.Controls.CameraMenuItemVisibility.Auto;
            this.pictureEdit1.Properties.SizeMode = DevExpress.XtraEditors.Controls.PictureSizeMode.Stretch;
            this.pictureEdit1.Properties.SvgImageSize = new System.Drawing.Size(80, 80);
            this.pictureEdit1.Size = new System.Drawing.Size(121, 117);
            this.pictureEdit1.TabIndex = 6;
            //
            // radioButton_White
            //
            this.radioButton_White.AutoSize = true;
            this.radioButton_White.Location = new System.Drawing.Point(40, 21);
            this.radioButton_White.Name = "radioButton_White";
            this.radioButton_White.Size = new System.Drawing.Size(75, 16);
            this.radioButton_White.TabIndex = 8;
            this.radioButton_White.Text = "흰색 배경";
            this.radioButton_White.UseVisualStyleBackColor = true;
            this.radioButton_White.CheckedChanged += new System.EventHandler(this.radioButton_White_CheckedChanged);
            //
            // radioButton_Black
            //
            this.radioButton_Black.AutoSize = true;
            this.radioButton_Black.Location = new System.Drawing.Point(167, 21);
            this.radioButton_Black.Name = "radioButton_Black";
            this.radioButton_Black.Size = new System.Drawing.Size(75, 16);
            this.radioButton_Black.TabIndex = 9;
            this.radioButton_Black.Text = "검정 배경";
            this.radioButton_Black.UseVisualStyleBackColor = true;
            this.radioButton_Black.CheckedChanged += new System.EventHandler(this.radioButton_Black_CheckedChanged);
            //
            // checkEdit_Showing
            //
            this.checkEdit_Showing.Location = new System.Drawing.Point(17, 212);
            this.checkEdit_Showing.Name = "checkEdit_Showing";
            this.checkEdit_Showing.Properties.Caption = "시작시 다시 열지 않음";
            this.checkEdit_Showing.Size = new System.Drawing.Size(133, 20);
            this.checkEdit_Showing.TabIndex = 10;
            this.checkEdit_Showing.CheckedChanged += new System.EventHandler(this.checkEdit_Showing_CheckedChanged);
            //
            // FormSkinSelect
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.DarkGray;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.ClientSize = new System.Drawing.Size(300, 234);
            this.ControlBox = false;
            this.Controls.Add(this.checkEdit_Showing);
            this.Controls.Add(this.radioButton_Black);
            this.Controls.Add(this.radioButton_White);
            this.Controls.Add(this.pictureEdit2);
            this.Controls.Add(this.pictureEdit1);
            this.Controls.Add(this.labelControl2);
            this.Controls.Add(this.labelControl1);
            this.Controls.Add(this.simpleButton_OK);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(300, 200);
            this.Name = "FormSkinSelect";
            this.Opacity = 0.8D;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "FormKeyIn";
            this.TopMost = true;
            this.Load += new System.EventHandler(this.FormSkinSelect_Load);
            ((System.ComponentModel.ISupportInitialize)(this.behaviorManager1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureEdit2.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureEdit1.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.checkEdit_Showing.Properties)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private DevExpress.Utils.Behaviors.BehaviorManager behaviorManager1;
        private DevExpress.XtraEditors.SimpleButton simpleButton_OK;
        private DevExpress.XtraEditors.LabelControl labelControl1;
        private DevExpress.XtraEditors.LabelControl labelControl2;
        private DevExpress.XtraEditors.PictureEdit pictureEdit1;
        private DevExpress.XtraEditors.PictureEdit pictureEdit2;
        private System.Windows.Forms.RadioButton radioButton_White;
        private System.Windows.Forms.RadioButton radioButton_Black;
        private DevExpress.XtraEditors.CheckEdit checkEdit_Showing;
    }
}