namespace OPC.DSClient.WinForm.UserControl
{
    partial class UcDsTextEdit
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
            richEditControl1 = new DevExpress.XtraRichEdit.RichEditControl();
            SuspendLayout();
            // 
            // richEditControl1
            // 
            richEditControl1.Dock = DockStyle.Fill;
            richEditControl1.Location = new Point(0, 0);
            richEditControl1.Name = "richEditControl1";
            richEditControl1.Options.DocumentSaveOptions.CurrentFormat = DevExpress.XtraRichEdit.DocumentFormat.PlainText;
            richEditControl1.Size = new Size(901, 483);
            richEditControl1.TabIndex = 0;
            // 
            // UcDsTextEdit
            // 
            AutoScaleDimensions = new SizeF(7F, 14F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(richEditControl1);
            Name = "UcDsTextEdit";
            Size = new Size(901, 483);
            ResumeLayout(false);
        }

        #endregion

        private DevExpress.XtraRichEdit.RichEditControl richEditControl1;
    }
}
