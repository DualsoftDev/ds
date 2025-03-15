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
            button_openDir = new Button();
            button_MelsecConvert = new Button();
            button_SiemensConvert = new Button();
            SuspendLayout();
            // 
            // button_openPLC
            // 
            button_openPLC.Location = new Point(108, 12);
            button_openPLC.Name = "button_openPLC";
            button_openPLC.Size = new Size(112, 23);
            button_openPLC.TabIndex = 0;
            button_openPLC.Text = "Open File";
            button_openPLC.UseVisualStyleBackColor = true;
            // 
            // button_openDir
            // 
            button_openDir.Location = new Point(226, 12);
            button_openDir.Name = "button_openDir";
            button_openDir.Size = new Size(112, 23);
            button_openDir.TabIndex = 0;
            button_openDir.Text = "Open Dir";
            button_openDir.UseVisualStyleBackColor = true;
            // 
            // button_MelsecConvert
            // 
            button_MelsecConvert.Location = new Point(344, 12);
            button_MelsecConvert.Name = "button_MelsecConvert";
            button_MelsecConvert.Size = new Size(112, 23);
            button_MelsecConvert.TabIndex = 0;
            button_MelsecConvert.Text = "Open Melsec";
            button_MelsecConvert.UseVisualStyleBackColor = true;
            // 
            // button_SiemensConvert
            // 
            button_SiemensConvert.Location = new Point(462, 12);
            button_SiemensConvert.Name = "button_SiemensConvert";
            button_SiemensConvert.Size = new Size(112, 23);
            button_SiemensConvert.TabIndex = 0;
            button_SiemensConvert.Text = "Open Siemens";
            button_SiemensConvert.UseVisualStyleBackColor = true;
            // 
            // FormMermaid
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1493, 1009);
            Controls.Add(button_SiemensConvert);
            Controls.Add(button_MelsecConvert);
            Controls.Add(button_openDir);
            Controls.Add(button_openPLC);
            Name = "FormMermaid";
            Text = "Form1";
            ResumeLayout(false);
        }

        #endregion

        private Button button_openPLC;
        private Button button_openDir;
        private Button button_MelsecConvert;
        private Button button_SiemensConvert;
    }
}
