namespace MelsecConverter
{
    partial class UcPanelLog
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
            this.components = new System.ComponentModel.Container();
            this.listBoxControlOutput = new DevExpress.XtraEditors.ListBoxControl();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.listBoxControlOutput)).BeginInit();
            this.SuspendLayout();
            // 
            // listBoxControlOutput
            // 
            this.listBoxControlOutput.ContextMenuStrip = this.contextMenuStrip1;
            this.listBoxControlOutput.Location = new System.Drawing.Point(39, 78);
            this.listBoxControlOutput.Name = "listBoxControlOutput";
            this.listBoxControlOutput.Size = new System.Drawing.Size(230, 95);
            this.listBoxControlOutput.TabIndex = 0;
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.ImageScalingSize = new System.Drawing.Size(18, 18);
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(61, 4);
            // 
            // UcPaneLog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.listBoxControlOutput);
            this.Name = "UcPaneLog";
            this.Size = new System.Drawing.Size(436, 238);
            this.Load += new System.EventHandler(this.UcPaneLog_Load);
            ((System.ComponentModel.ISupportInitialize)(this.listBoxControlOutput)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private DevExpress.XtraEditors.ListBoxControl listBoxControlOutput;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
    }
}
