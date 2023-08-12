
namespace DSModeler
{
    partial class UcLog
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(UcLog));
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.copyBtn = new DevExpress.XtraEditors.SimpleButton();
            this.clearBtn = new DevExpress.XtraEditors.SimpleButton();
            this.copyAllBtn = new DevExpress.XtraEditors.SimpleButton();
            this.listBoxControlOutput = new DevExpress.XtraEditors.ListBoxControl();
            this.logLevelBtn = new DevExpress.XtraEditors.SimpleButton();
            ((System.ComponentModel.ISupportInitialize)(this.listBoxControlOutput)).BeginInit();
            this.SuspendLayout();
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(61, 4);
            // 
            // copyBtn
            // 
            this.copyBtn.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("copyBtn.ImageOptions.Image")));
            this.copyBtn.Location = new System.Drawing.Point(287, 24);
            this.copyBtn.Name = "copyBtn";
            this.copyBtn.Size = new System.Drawing.Size(111, 34);
            this.copyBtn.TabIndex = 1;
            this.copyBtn.Text = "copy";
            this.copyBtn.Visible = false;
            // 
            // clearBtn
            // 
            this.clearBtn.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("clearBtn.ImageOptions.Image")));
            this.clearBtn.Location = new System.Drawing.Point(287, 100);
            this.clearBtn.Name = "clearBtn";
            this.clearBtn.Size = new System.Drawing.Size(111, 34);
            this.clearBtn.TabIndex = 1;
            this.clearBtn.Text = "clear";
            this.clearBtn.Visible = false;
            // 
            // copyAllBtn
            // 
            this.copyAllBtn.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("copyAllBtn.ImageOptions.Image")));
            this.copyAllBtn.Location = new System.Drawing.Point(287, 61);
            this.copyAllBtn.Name = "copyAllBtn";
            this.copyAllBtn.Size = new System.Drawing.Size(111, 34);
            this.copyAllBtn.TabIndex = 2;
            this.copyAllBtn.Text = "copyAll";
            this.copyAllBtn.Visible = false;
            // 
            // listBoxControlOutput
            // 
            this.listBoxControlOutput.ContextMenuStrip = this.contextMenuStrip1;
            this.listBoxControlOutput.Location = new System.Drawing.Point(20, 24);
            this.listBoxControlOutput.Margin = new System.Windows.Forms.Padding(2);
            this.listBoxControlOutput.Name = "listBoxControlOutput";
            this.listBoxControlOutput.Size = new System.Drawing.Size(262, 150);
            this.listBoxControlOutput.TabIndex = 3;
            // 
            // logLevelBtn
            // 
            this.logLevelBtn.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("logLevelBtn.ImageOptions.Image")));
            this.logLevelBtn.Location = new System.Drawing.Point(287, 140);
            this.logLevelBtn.Name = "logLevelBtn";
            this.logLevelBtn.Size = new System.Drawing.Size(111, 34);
            this.logLevelBtn.TabIndex = 4;
            this.logLevelBtn.Text = "logLevel";
            this.logLevelBtn.Visible = false;
            // 
            // UcLog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.logLevelBtn);
            this.Controls.Add(this.listBoxControlOutput);
            this.Controls.Add(this.copyAllBtn);
            this.Controls.Add(this.clearBtn);
            this.Controls.Add(this.copyBtn);
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "UcLog";
            this.Size = new System.Drawing.Size(435, 197);
            this.Load += new System.EventHandler(this.UcLog_Load);
            ((System.ComponentModel.ISupportInitialize)(this.listBoxControlOutput)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private DevExpress.XtraEditors.SimpleButton copyBtn;
        private DevExpress.XtraEditors.SimpleButton clearBtn;
        private DevExpress.XtraEditors.SimpleButton copyAllBtn;
        private DevExpress.XtraEditors.ListBoxControl listBoxControlOutput;
        private DevExpress.XtraEditors.SimpleButton logLevelBtn;
    }
}
