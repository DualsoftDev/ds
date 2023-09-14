namespace PowerPointAddInForDS

{
    partial class FormDocText
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormDocText));
            this.TextEdit = new System.Windows.Forms.RichTextBox();
            this.SuspendLayout();
            // 
            // richTextBox_ds
            // 
            this.TextEdit.AcceptsTab = true;
            this.TextEdit.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(33)))), ((int)(((byte)(33)))));
            this.TextEdit.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TextEdit.ForeColor = System.Drawing.Color.BlanchedAlmond;
            this.TextEdit.Location = new System.Drawing.Point(0, 0);
            this.TextEdit.Name = "richTextBox_ds";
            this.TextEdit.ReadOnly = true;
            this.TextEdit.Size = new System.Drawing.Size(694, 570);
            this.TextEdit.TabIndex = 3;
            this.TextEdit.Text = "";
            // 
            // FormDocText
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(694, 570);
            this.Controls.Add(this.TextEdit);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "FormDocText";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Dualsoft Language Text";
            this.ResumeLayout(false);

        }

        #endregion

    }
}