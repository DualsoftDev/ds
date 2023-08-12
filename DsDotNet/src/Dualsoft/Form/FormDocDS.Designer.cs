namespace DSModeler.Form
{
    partial class FormDocDS
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
            this.memoEdit_DS = new DevExpress.XtraEditors.MemoEdit();
            ((System.ComponentModel.ISupportInitialize)(this.memoEdit_DS.Properties)).BeginInit();
            this.SuspendLayout();
            // 
            // memoEdit_DS
            // 
            this.memoEdit_DS.Dock = System.Windows.Forms.DockStyle.Fill;
            this.memoEdit_DS.Location = new System.Drawing.Point(0, 0);
            this.memoEdit_DS.Name = "memoEdit_DS";
            this.memoEdit_DS.Size = new System.Drawing.Size(694, 665);
            this.memoEdit_DS.TabIndex = 0;
            // 
            // FormDocDS
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(694, 665);
            this.Controls.Add(this.memoEdit_DS);
            this.Name = "FormDocDS";
            this.Text = "Start";
            ((System.ComponentModel.ISupportInitialize)(this.memoEdit_DS.Properties)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private DevExpress.XtraEditors.MemoEdit memoEdit_DS;
    }
}