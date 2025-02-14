namespace Dual.Common.Winform
{
    partial class FormException
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
            if(disposing && (components != null))
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
            this.buttonException = new System.Windows.Forms.Button();
            this.buttonClose = new System.Windows.Forms.Button();
            this.labelInfo = new System.Windows.Forms.Label();
            this.SuspendLayout();
            //
            // buttonException
            //
            this.buttonException.Location = new System.Drawing.Point(12, 88);
            this.buttonException.Name = "buttonException";
            this.buttonException.Size = new System.Drawing.Size(197, 23);
            this.buttonException.TabIndex = 0;
            this.buttonException.Text = "Generate Exception";
            this.buttonException.UseVisualStyleBackColor = true;
            this.buttonException.Click += new System.EventHandler(this.buttonException_Click);
            //
            // buttonClose
            //
            this.buttonClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonClose.Location = new System.Drawing.Point(12, 118);
            this.buttonClose.Name = "buttonClose";
            this.buttonClose.Size = new System.Drawing.Size(197, 23);
            this.buttonClose.TabIndex = 1;
            this.buttonClose.Text = "Close";
            this.buttonClose.UseVisualStyleBackColor = true;
            //
            // labelInfo
            //
            this.labelInfo.Location = new System.Drawing.Point(13, 13);
            this.labelInfo.Name = "labelInfo";
            this.labelInfo.Size = new System.Drawing.Size(196, 72);
            this.labelInfo.TabIndex = 2;
            this.labelInfo.Text = "Press to \"Generate Exception\" button to raise an exception.\r\nNote: in this case t" +
                "he exception won\'t terminate the App but you should be prepared for worst scenar" +
                "io.";
            //
            // exceptionForm
            //
            this.AcceptButton = this.buttonException;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.buttonClose;
            this.ClientSize = new System.Drawing.Size(222, 151);
            this.Controls.Add(this.labelInfo);
            this.Controls.Add(this.buttonClose);
            this.Controls.Add(this.buttonException);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "exceptionForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Exception Demo";
            this.TopMost = true;
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button buttonException;
        private System.Windows.Forms.Button buttonClose;
        private System.Windows.Forms.Label labelInfo;
    }
}