namespace WinFormsApp1
{
    partial class ThreadedForm
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
            textBox1 = new System.Windows.Forms.TextBox();
            textBoxState = new System.Windows.Forms.TextBox();
            SuspendLayout();
            //
            // textBox1
            //
            textBox1.Location = new System.Drawing.Point(86, 94);
            textBox1.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            textBox1.Name = "textBox1";
            textBox1.Size = new System.Drawing.Size(132, 27);
            textBox1.TabIndex = 0;
            //
            // textBoxState
            //
            textBoxState.Location = new System.Drawing.Point(86, 199);
            textBoxState.Name = "textBoxState";
            textBoxState.Size = new System.Drawing.Size(125, 27);
            textBoxState.TabIndex = 1;
            //
            // ThreadedForm
            //
            AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(261, 257);
            Controls.Add(textBoxState);
            Controls.Add(textBox1);
            Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            Name = "ThreadedForm";
            Text = "ThreadedForm";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.TextBox textBoxState;
    }
}