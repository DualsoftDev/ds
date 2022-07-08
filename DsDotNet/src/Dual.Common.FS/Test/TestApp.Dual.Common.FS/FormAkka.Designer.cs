
namespace TestApp.Dual.Common.FS
{
    partial class FormAkka
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
            this.btnLaunchServer = new System.Windows.Forms.Button();
            this.btnLaunchClient = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.textBoxIp = new System.Windows.Forms.TextBox();
            this.textBoxPort = new System.Windows.Forms.TextBox();
            this.btnTerminateClient = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btnLaunchServer
            // 
            this.btnLaunchServer.Location = new System.Drawing.Point(31, 21);
            this.btnLaunchServer.Name = "btnLaunchServer";
            this.btnLaunchServer.Size = new System.Drawing.Size(141, 48);
            this.btnLaunchServer.TabIndex = 0;
            this.btnLaunchServer.Text = "Launch Server";
            this.btnLaunchServer.UseVisualStyleBackColor = true;
            this.btnLaunchServer.Click += new System.EventHandler(this.btnLaunchServer_Click);
            // 
            // btnLaunchClient
            // 
            this.btnLaunchClient.Location = new System.Drawing.Point(31, 187);
            this.btnLaunchClient.Name = "btnLaunchClient";
            this.btnLaunchClient.Size = new System.Drawing.Size(141, 48);
            this.btnLaunchClient.TabIndex = 1;
            this.btnLaunchClient.Text = "Launch Client";
            this.btnLaunchClient.UseVisualStyleBackColor = true;
            this.btnLaunchClient.Click += new System.EventHandler(this.btnLaunchClient_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(48, 91);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(28, 18);
            this.label1.TabIndex = 3;
            this.label1.Text = "IP:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(48, 125);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(47, 18);
            this.label2.TabIndex = 5;
            this.label2.Text = "Port:";
            // 
            // textBoxIp
            // 
            this.textBoxIp.Location = new System.Drawing.Point(97, 81);
            this.textBoxIp.Name = "textBoxIp";
            this.textBoxIp.Size = new System.Drawing.Size(225, 28);
            this.textBoxIp.TabIndex = 6;
            // 
            // textBoxPort
            // 
            this.textBoxPort.Location = new System.Drawing.Point(97, 115);
            this.textBoxPort.Name = "textBoxPort";
            this.textBoxPort.Size = new System.Drawing.Size(225, 28);
            this.textBoxPort.TabIndex = 7;
            // 
            // btnTerminateClient
            // 
            this.btnTerminateClient.Location = new System.Drawing.Point(181, 187);
            this.btnTerminateClient.Name = "btnTerminateClient";
            this.btnTerminateClient.Size = new System.Drawing.Size(141, 48);
            this.btnTerminateClient.TabIndex = 8;
            this.btnTerminateClient.Text = "Terminate Client";
            this.btnTerminateClient.UseVisualStyleBackColor = true;
            // 
            // FormAkka
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(418, 303);
            this.Controls.Add(this.btnTerminateClient);
            this.Controls.Add(this.textBoxPort);
            this.Controls.Add(this.textBoxIp);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btnLaunchClient);
            this.Controls.Add(this.btnLaunchServer);
            this.Name = "FormAkka";
            this.Text = "FormAkka";
            this.Load += new System.EventHandler(this.FormAkka_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnLaunchServer;
        private System.Windows.Forms.Button btnLaunchClient;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBoxIp;
        private System.Windows.Forms.TextBox textBoxPort;
        private System.Windows.Forms.Button btnTerminateClient;
    }
}