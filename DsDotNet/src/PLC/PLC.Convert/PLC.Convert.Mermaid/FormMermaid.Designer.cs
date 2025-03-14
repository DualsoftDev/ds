namespace PLC.Convert.Mermaid
{
    partial class FormMermaid
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            button_openPLC = new Button();
            SuspendLayout();
            // 
            // button_openPLC
            // 
            button_openPLC.Location = new Point(14, 4);
            button_openPLC.Name = "button_openPLC";
            button_openPLC.Size = new Size(75, 23);
            button_openPLC.TabIndex = 0;
            button_openPLC.Text = "Open";
            button_openPLC.UseVisualStyleBackColor = true;
            // 
            // FormMermaid
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(button_openPLC);
            Name = "FormMermaid";
            Text = "Form1";
            ResumeLayout(false);
        }

        #endregion

        private Button button_openPLC;
    }
}
