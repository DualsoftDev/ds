
namespace Model.DsEditor
{
    partial class FormMain
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
        /// 이 메서드의 내용을 코드 편집기로 수정하지 마세요.
        /// </summary>
        private void InitializeComponent()
        {
            this.richTextBox_Debug = new System.Windows.Forms.RichTextBox();
            this.SuspendLayout();
            // 
            // richTextBox_Debug
            // 
            this.richTextBox_Debug.Dock = System.Windows.Forms.DockStyle.Fill;
            this.richTextBox_Debug.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.richTextBox_Debug.Location = new System.Drawing.Point(0, 0);
            this.richTextBox_Debug.Name = "richTextBox_Debug";
            this.richTextBox_Debug.ReadOnly = true;
            this.richTextBox_Debug.Size = new System.Drawing.Size(1079, 771);
            this.richTextBox_Debug.TabIndex = 1;
            this.richTextBox_Debug.Text = "";
            // 
            // FormMain
            // 
            this.ClientSize = new System.Drawing.Size(1079, 771);
            this.Controls.Add(this.richTextBox_Debug);
            this.Name = "FormMain";
            this.Load += new System.EventHandler(this.FormMain_Load);
            this.ResumeLayout(false);

        }


        #endregion

        private System.Windows.Forms.RichTextBox richTextBox_Debug;
    }
}

