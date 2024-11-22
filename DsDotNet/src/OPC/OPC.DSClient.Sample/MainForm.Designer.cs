/* ========================================================================
 * Copyright (c) 2005-2020 The OPC Foundation, Inc. All rights reserved.
 *
 * OPC Foundation MIT License 1.00
 * 
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 * 
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * The complete license agreement can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 * ======================================================================*/

using Opc.Ua.Client.Controls;

namespace OPC.DSClient
{
    partial class MainForm
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
            components = new System.ComponentModel.Container();
            MenuBar = new MenuStrip();
            fileToolStripMenuItem = new ToolStripMenuItem();
            exitToolStripMenuItem = new ToolStripMenuItem();
            ServerMI = new ToolStripMenuItem();
            Server_DiscoverMI = new ToolStripMenuItem();
            Server_ConnectMI = new ToolStripMenuItem();
            Server_DisconnectMI = new ToolStripMenuItem();
            HelpMI = new ToolStripMenuItem();
            Help_ContentsMI = new ToolStripMenuItem();
            StatusBar = new StatusStrip();
            ConnectServerCTRL = new ConnectServerCtrl();
            BrowseCTRL = new BrowseNodeCtrl();
            clientHeaderBranding1 = new HeaderBranding();
            tagViewerToolStripMenuItem = new ToolStripMenuItem();
            MenuBar.SuspendLayout();
            SuspendLayout();
            // 
            // MenuBar
            // 
            MenuBar.ImageScalingSize = new Size(18, 18);
            MenuBar.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem, ServerMI, HelpMI });
            MenuBar.Location = new Point(0, 0);
            MenuBar.Name = "MenuBar";
            MenuBar.Padding = new Padding(7, 3, 0, 3);
            MenuBar.Size = new Size(1031, 27);
            MenuBar.TabIndex = 1;
            MenuBar.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { exitToolStripMenuItem });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(39, 21);
            fileToolStripMenuItem.Text = "&File";
            // 
            // exitToolStripMenuItem
            // 
            exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            exitToolStripMenuItem.Size = new Size(102, 24);
            exitToolStripMenuItem.Text = "E&xit";
            exitToolStripMenuItem.Click += ExitToolStripMenuItem_Click;
            // 
            // ServerMI
            // 
            ServerMI.DropDownItems.AddRange(new ToolStripItem[] { Server_DiscoverMI, Server_ConnectMI, Server_DisconnectMI, tagViewerToolStripMenuItem });
            ServerMI.Name = "ServerMI";
            ServerMI.Size = new Size(57, 21);
            ServerMI.Text = "Server";
            // 
            // Server_DiscoverMI
            // 
            Server_DiscoverMI.Name = "Server_DiscoverMI";
            Server_DiscoverMI.Size = new Size(198, 24);
            Server_DiscoverMI.Text = "Discover...";
            Server_DiscoverMI.Click += Server_DiscoverMI_Click;
            // 
            // Server_ConnectMI
            // 
            Server_ConnectMI.Name = "Server_ConnectMI";
            Server_ConnectMI.Size = new Size(198, 24);
            Server_ConnectMI.Text = "Connect";
            Server_ConnectMI.Click += Server_ConnectMI_Click;
            // 
            // Server_DisconnectMI
            // 
            Server_DisconnectMI.Name = "Server_DisconnectMI";
            Server_DisconnectMI.Size = new Size(198, 24);
            Server_DisconnectMI.Text = "Disconnect";
            Server_DisconnectMI.Click += Server_DisconnectMI_Click;
            // 
            // HelpMI
            // 
            HelpMI.DropDownItems.AddRange(new ToolStripItem[] { Help_ContentsMI });
            HelpMI.Name = "HelpMI";
            HelpMI.Size = new Size(47, 21);
            HelpMI.Text = "Help";
            // 
            // Help_ContentsMI
            // 
            Help_ContentsMI.Name = "Help_ContentsMI";
            Help_ContentsMI.Size = new Size(135, 24);
            Help_ContentsMI.Text = "Contents";
            Help_ContentsMI.Click += Help_ContentsMI_Click;
            // 
            // StatusBar
            // 
            StatusBar.ImageScalingSize = new Size(18, 18);
            StatusBar.Location = new Point(0, 692);
            StatusBar.Name = "StatusBar";
            StatusBar.Padding = new Padding(1, 0, 16, 0);
            StatusBar.Size = new Size(1031, 22);
            StatusBar.TabIndex = 2;
            // 
            // ConnectServerCTRL
            // 
            ConnectServerCTRL.Configuration = null;
            ConnectServerCTRL.DisableDomainCheck = false;
            ConnectServerCTRL.DiscoverTimeout = 15000;
            ConnectServerCTRL.Dock = DockStyle.Top;
            ConnectServerCTRL.Location = new Point(0, 125);
            ConnectServerCTRL.Margin = new Padding(5, 5, 5, 5);
            ConnectServerCTRL.MaximumSize = new Size(2389, 30);
            ConnectServerCTRL.MinimumSize = new Size(583, 30);
            ConnectServerCTRL.Name = "ConnectServerCTRL";
            ConnectServerCTRL.PreferredLocales = null;
            ConnectServerCTRL.ReconnectPeriod = 1;
            ConnectServerCTRL.ServerUrl = "";
            ConnectServerCTRL.SessionName = null;
            ConnectServerCTRL.SessionTimeout = 60000U;
            ConnectServerCTRL.Size = new Size(1031, 30);
            ConnectServerCTRL.StatusStrip = StatusBar;
            ConnectServerCTRL.TabIndex = 6;
            ConnectServerCTRL.UserIdentity = null;
            ConnectServerCTRL.UseSecurity = true;
            ConnectServerCTRL.ReconnectStarting += Server_ReconnectStarting;
            ConnectServerCTRL.ReconnectComplete += Server_ReconnectComplete;
            ConnectServerCTRL.ConnectComplete += Server_ConnectComplete;
            // 
            // BrowseCTRL
            // 
            BrowseCTRL.AttributesListCollapsed = false;
            BrowseCTRL.Dock = DockStyle.Fill;
            BrowseCTRL.Location = new Point(0, 155);
            BrowseCTRL.Margin = new Padding(5, 5, 5, 5);
            BrowseCTRL.Name = "BrowseCTRL";
            BrowseCTRL.Size = new Size(1031, 537);
            BrowseCTRL.SplitterDistance = 387;
            BrowseCTRL.TabIndex = 5;
            BrowseCTRL.View = null;
            // 
            // clientHeaderBranding1
            // 
            clientHeaderBranding1.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            clientHeaderBranding1.BackColor = Color.White;
            clientHeaderBranding1.Dock = DockStyle.Top;
            clientHeaderBranding1.Location = new Point(0, 27);
            clientHeaderBranding1.Margin = new Padding(4, 4, 4, 4);
            clientHeaderBranding1.MaximumSize = new Size(0, 98);
            clientHeaderBranding1.MinimumSize = new Size(583, 98);
            clientHeaderBranding1.Name = "clientHeaderBranding1";
            clientHeaderBranding1.Padding = new Padding(4, 4, 4, 4);
            clientHeaderBranding1.Size = new Size(1031, 98);
            clientHeaderBranding1.TabIndex = 7;
            // 
            // tagViewerToolStripMenuItem
            // 
            tagViewerToolStripMenuItem.Name = "tagViewerToolStripMenuItem";
            tagViewerToolStripMenuItem.Size = new Size(198, 24);
            tagViewerToolStripMenuItem.Text = "Tag Viewer";
            tagViewerToolStripMenuItem.Visible = false;
            tagViewerToolStripMenuItem.Click += tagViewerToolStripMenuItem_Click;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1031, 714);
            Controls.Add(BrowseCTRL);
            Controls.Add(ConnectServerCTRL);
            Controls.Add(StatusBar);
            Controls.Add(clientHeaderBranding1);
            Controls.Add(MenuBar);
            MainMenuStrip = MenuBar;
            Margin = new Padding(4, 4, 4, 4);
            Name = "MainForm";
            Text = "Quickstart Reference Client";
            FormClosing += MainForm_FormClosing;
            MenuBar.ResumeLayout(false);
            MenuBar.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.MenuStrip MenuBar;
        private System.Windows.Forms.StatusStrip StatusBar;
        private System.Windows.Forms.ToolStripMenuItem ServerMI;
        private System.Windows.Forms.ToolStripMenuItem Server_DiscoverMI;
        private System.Windows.Forms.ToolStripMenuItem Server_ConnectMI;
        private System.Windows.Forms.ToolStripMenuItem Server_DisconnectMI;
        private System.Windows.Forms.ToolStripMenuItem HelpMI;
        private System.Windows.Forms.ToolStripMenuItem Help_ContentsMI;
        private ConnectServerCtrl ConnectServerCTRL;
        private BrowseNodeCtrl BrowseCTRL;
        private HeaderBranding clientHeaderBranding1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private ToolStripMenuItem tagViewerToolStripMenuItem;
    }
}
