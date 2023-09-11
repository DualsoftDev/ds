using Diagram.View.MSAGL;

namespace PowerPointAddInForDS
{
    partial class FormDocView
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormDocView));
            this.ucView1 = new UcView();
            this.SuspendLayout();
            // 
            // ucView1
            // 
            this.ucView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ucView1.Flow = null;
            this.ucView1.Location = new System.Drawing.Point(0, 0);
            this.ucView1.MasterNode = null;
            this.ucView1.Name = "ucView1";
            this.ucView1.Size = new System.Drawing.Size(694, 570);
            this.ucView1.TabIndex = 0;
            // 
            // FormDocView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(694, 570);
            this.Controls.Add(this.ucView1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "FormDocView";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Dualsoft Language  Graph";
            this.ResumeLayout(false);

        }

        #endregion

        private UcView ucView1;
    }
}