namespace OPC.DSClient.WinForm.UserControl
{
    partial class UcDsTree
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
            treeList1 = new DevExpress.XtraTreeList.TreeList();
            ((System.ComponentModel.ISupportInitialize)treeList1).BeginInit();
            SuspendLayout();
            // 
            // treeList1
            // 
            treeList1.Dock = DockStyle.Fill;
            treeList1.Location = new Point(0, 0);
            treeList1.Name = "treeList1";
            treeList1.Size = new Size(898, 692);
            treeList1.TabIndex = 0;
            // 
            // UcDsTree
            // 
            AutoScaleDimensions = new SizeF(7F, 16F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(treeList1);
            Name = "UcDsTree";
            Size = new Size(898, 692);
            ((System.ComponentModel.ISupportInitialize)treeList1).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private DevExpress.XtraTreeList.TreeList treeList1;
    }
}
