namespace WinFormsApp1
{
    partial class FormMain
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
            btnCreateThreadedForm = new System.Windows.Forms.Button();
            btnInvoke = new System.Windows.Forms.Button();
            textBoxState = new System.Windows.Forms.TextBox();
            SuspendLayout();
            // 
            // btnCreateThreadedForm
            // 
            btnCreateThreadedForm.Location = new System.Drawing.Point(64, 38);
            btnCreateThreadedForm.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            btnCreateThreadedForm.Name = "btnCreateThreadedForm";
            btnCreateThreadedForm.Size = new System.Drawing.Size(161, 31);
            btnCreateThreadedForm.TabIndex = 0;
            btnCreateThreadedForm.Text = "Threaded new form";
            btnCreateThreadedForm.UseVisualStyleBackColor = true;
            btnCreateThreadedForm.Click += btnCreateThreadedForm_Click;
            // 
            // btnInvoke
            // 
            btnInvoke.Location = new System.Drawing.Point(225, 38);
            btnInvoke.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            btnInvoke.Name = "btnInvoke";
            btnInvoke.Size = new System.Drawing.Size(101, 31);
            btnInvoke.TabIndex = 1;
            btnInvoke.Text = "Invoke";
            btnInvoke.UseVisualStyleBackColor = true;
            btnInvoke.Click += btnInvoke_Click;
            // 
            // textBoxState
            // 
            textBoxState.Location = new System.Drawing.Point(29, 162);
            textBoxState.Name = "textBoxState";
            textBoxState.Size = new System.Drawing.Size(125, 27);
            textBoxState.TabIndex = 2;
            // 
            // FormMain
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(374, 220);
            Controls.Add(textBoxState);
            Controls.Add(btnInvoke);
            Controls.Add(btnCreateThreadedForm);
            Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            Name = "FormMain";
            Text = "Form1";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Button btnCreateThreadedForm;
        private System.Windows.Forms.Button btnInvoke;
        private System.Windows.Forms.TextBox textBoxState;
    }
}
