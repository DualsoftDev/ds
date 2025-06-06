namespace DSModeler
{
    partial class FormProperties
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
            splitContainer1 = new SplitContainer();
            treeView1 = new TreeView();
            menuStrip1 = new MenuStrip();
            fileToolStripMenuItem = new ToolStripMenuItem();
            openToolStripMenuItem = new ToolStripMenuItem();
            exportJsonToolStripMenuItem = new ToolStripMenuItem();
            importJsonToolStripMenuItem = new ToolStripMenuItem();
            propertyGrid1 = new PropertyGrid();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            menuStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.Location = new Point(0, 0);
            splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(treeView1);
            splitContainer1.Panel1.Controls.Add(menuStrip1);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(propertyGrid1);
            splitContainer1.Size = new Size(800, 397);
            splitContainer1.SplitterDistance = 266;
            splitContainer1.TabIndex = 0;
            // 
            // treeView1
            // 
            treeView1.Location = new Point(0, 21);
            treeView1.Name = "treeView1";
            treeView1.Size = new Size(266, 376);
            treeView1.TabIndex = 0;
            // 
            // menuStrip1
            // 
            menuStrip1.ImageScalingSize = new Size(18, 18);
            menuStrip1.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(266, 24);
            menuStrip1.TabIndex = 1;
            menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { openToolStripMenuItem, exportJsonToolStripMenuItem, importJsonToolStripMenuItem });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(37, 20);
            fileToolStripMenuItem.Text = "File";
            // 
            // openToolStripMenuItem
            // 
            openToolStripMenuItem.Name = "openToolStripMenuItem";
            openToolStripMenuItem.Size = new Size(137, 22);
            openToolStripMenuItem.Text = "Open (F4)";
            openToolStripMenuItem.Click += OpenToolStripMenuItem_Click;
            // 
            // exportJsonToolStripMenuItem
            // 
            exportJsonToolStripMenuItem.Name = "exportJsonToolStripMenuItem";
            exportJsonToolStripMenuItem.Size = new Size(137, 22);
            exportJsonToolStripMenuItem.Text = "Export Json";
            exportJsonToolStripMenuItem.Click += exportJsonToolStripMenuItem_Click;
            // 
            // importJsonToolStripMenuItem
            // 
            importJsonToolStripMenuItem.Name = "importJsonToolStripMenuItem";
            importJsonToolStripMenuItem.Size = new Size(137, 22);
            importJsonToolStripMenuItem.Text = "Import Json";
            importJsonToolStripMenuItem.Click += importJsonToolStripMenuItem_Click;
            // 
            // propertyGrid1
            // 
            propertyGrid1.Dock = DockStyle.Fill;
            propertyGrid1.Location = new Point(0, 0);
            propertyGrid1.Name = "propertyGrid1";
            propertyGrid1.Size = new Size(530, 397);
            propertyGrid1.TabIndex = 0;
            // 
            // FormProperties
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 397);
            Controls.Add(splitContainer1);
            MainMenuStrip = menuStrip1;
            Name = "FormProperties";
            Text = "DsProperties";
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel1.PerformLayout();
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            ResumeLayout(false);
        }



        #endregion

        private SplitContainer splitContainer1;
        private TreeView treeView1;
        private PropertyGrid propertyGrid1;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem openToolStripMenuItem;
        private ToolStripMenuItem exportJsonToolStripMenuItem;
        private ToolStripMenuItem importJsonToolStripMenuItem;
    }
}
