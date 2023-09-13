using Diagram.View.MSAGL;

namespace DSModeler.Form
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
            this.UcView = new UcView();
            this.SuspendLayout();
            // 
            // ucView1
            // 
            this.UcView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.UcView.Flow = null;
            this.UcView.Location = new System.Drawing.Point(0, 0);
            this.UcView.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.UcView.MasterNode = null;
            this.UcView.Name = "ucView1";
            this.UcView.Size = new System.Drawing.Size(694, 665);
            this.UcView.TabIndex = 0;
            // 
            // FormDocView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(694, 665);
            this.Controls.Add(this.UcView);
            this.Name = "FormDocView";
            this.Text = "FormDocView";
            this.ResumeLayout(false);

        }

        #endregion

    }
}