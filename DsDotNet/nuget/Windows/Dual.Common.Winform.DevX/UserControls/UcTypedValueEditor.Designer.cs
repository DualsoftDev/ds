namespace Dual.Common.Winform.DevX
{
    partial class UcTypedValueEditor
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
            this.comboDataTypeSelector = new DevExpress.XtraEditors.ComboBoxEdit();
            this.textEditValue = new DevExpress.XtraEditors.TextEdit();
            ((System.ComponentModel.ISupportInitialize)(this.comboDataTypeSelector.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.textEditValue.Properties)).BeginInit();
            this.SuspendLayout();
            // 
            // comboDataTypeSelector
            // 
            this.comboDataTypeSelector.Location = new System.Drawing.Point(0, 0);
            this.comboDataTypeSelector.Margin = new System.Windows.Forms.Padding(4);
            this.comboDataTypeSelector.Name = "comboDataTypeSelector";
            this.comboDataTypeSelector.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.comboDataTypeSelector.Size = new System.Drawing.Size(100, 40);
            this.comboDataTypeSelector.TabIndex = 0;
            // 
            // textEditValue
            // 
            this.textEditValue.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textEditValue.Location = new System.Drawing.Point(108, 0);
            this.textEditValue.Margin = new System.Windows.Forms.Padding(4);
            this.textEditValue.Name = "textEditValue";
            this.textEditValue.Size = new System.Drawing.Size(215, 40);
            this.textEditValue.TabIndex = 1;
            // 
            // UcTypedValueEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.textEditValue);
            this.Controls.Add(this.comboDataTypeSelector);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "UcTypedValueEditor";
            this.Size = new System.Drawing.Size(327, 40);
            this.Load += new System.EventHandler(this.UcTypedValueEditor_Load);
            ((System.ComponentModel.ISupportInitialize)(this.comboDataTypeSelector.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.textEditValue.Properties)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private DevExpress.XtraEditors.ComboBoxEdit comboDataTypeSelector;
        private DevExpress.XtraEditors.TextEdit textEditValue;
    }
}
