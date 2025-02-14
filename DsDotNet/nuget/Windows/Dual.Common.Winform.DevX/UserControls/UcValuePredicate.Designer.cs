namespace Dual.Common.Winform.DevX.UserControls
{
    partial class UcValuePredicate
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
            this.tbExpression = new DevExpress.XtraEditors.TextEdit();
            this.comboDataTypeSelector = new DevExpress.XtraEditors.ComboBoxEdit();
            ((System.ComponentModel.ISupportInitialize)(this.tbExpression.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.comboDataTypeSelector.Properties)).BeginInit();
            this.SuspendLayout();
            // 
            // tbExpression
            // 
            this.tbExpression.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbExpression.Location = new System.Drawing.Point(108, -1);
            this.tbExpression.Margin = new System.Windows.Forms.Padding(4);
            this.tbExpression.Name = "tbExpression";
            this.tbExpression.Size = new System.Drawing.Size(138, 40);
            this.tbExpression.TabIndex = 3;
            this.tbExpression.ToolTip = "x == true\r\nx == false\r\n3 < x <= 5\r\nx > 10";
            // 
            // comboDataTypeSelector
            // 
            this.comboDataTypeSelector.Location = new System.Drawing.Point(0, -1);
            this.comboDataTypeSelector.Margin = new System.Windows.Forms.Padding(4);
            this.comboDataTypeSelector.Name = "comboDataTypeSelector";
            this.comboDataTypeSelector.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.comboDataTypeSelector.Size = new System.Drawing.Size(100, 40);
            this.comboDataTypeSelector.TabIndex = 4;
            // 
            // UcValuePredicate
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.comboDataTypeSelector);
            this.Controls.Add(this.tbExpression);
            this.Name = "UcValuePredicate";
            this.Size = new System.Drawing.Size(246, 40);
            this.Load += new System.EventHandler(this.UcValuePredicate_Load);
            ((System.ComponentModel.ISupportInitialize)(this.tbExpression.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.comboDataTypeSelector.Properties)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private DevExpress.XtraEditors.TextEdit tbExpression;
        private DevExpress.XtraEditors.ComboBoxEdit comboDataTypeSelector;
    }
}
