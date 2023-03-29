namespace XGCommLibTest
{
    partial class XGCommLib20Test
    {
        /// <summary>
        /// 필수 디자이너 변수입니다.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 사용 중인 모든 리소스를 정리합니다.
        /// </summary>
        /// <param name="disposing">관리되는 리소스를 삭제해야 하면 true이고, 그렇지 않으면 false입니다.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form 디자이너에서 생성한 코드

        /// <summary>
        /// 디자이너 지원에 필요한 메서드입니다.
        /// 이 메서드의 내용을 코드 편집기로 수정하지 마십시오.
        /// </summary>
        private void InitializeComponent()
        {
            this.Connect = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.btnBlock = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // Connect
            // 
            this.Connect.Location = new System.Drawing.Point(17, 34);
            this.Connect.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Connect.Name = "Connect";
            this.Connect.Size = new System.Drawing.Size(143, 39);
            this.Connect.TabIndex = 0;
            this.Connect.Text = "USB";
            this.Connect.UseVisualStyleBackColor = true;
            this.Connect.Click += new System.EventHandler(this.Connect_Click);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(17, 99);
            this.button1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(143, 39);
            this.button1.TabIndex = 1;
            this.button1.Text = "Ethernet";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.Connect_ETH_Click);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(17, 174);
            this.button2.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(143, 39);
            this.button2.TabIndex = 2;
            this.button2.Text = "XG";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // btnBlock
            // 
            this.btnBlock.Location = new System.Drawing.Point(183, 244);
            this.btnBlock.Margin = new System.Windows.Forms.Padding(4);
            this.btnBlock.Name = "btnBlock";
            this.btnBlock.Size = new System.Drawing.Size(143, 39);
            this.btnBlock.TabIndex = 3;
            this.btnBlock.Text = "Block";
            this.btnBlock.UseVisualStyleBackColor = true;
            this.btnBlock.Click += new System.EventHandler(this.btnBlock_Click);
            // 
            // XGCommLib20Test
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(406, 393);
            this.Controls.Add(this.btnBlock);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.Connect);
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Name = "XGCommLib20Test";
            this.Text = "XGCommLib20Test";
            this.Load += new System.EventHandler(this.XGCommLib20Test_Load);
            this.Click += new System.EventHandler(this.Connect_ETH_Click);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button Connect;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button btnBlock;
    }
}

