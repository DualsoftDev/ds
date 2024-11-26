using DevExpress.XtraEditors;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Forms;

namespace OPC.DSClient
{
    public static class Global
    {
        public static XtraUserControl? SelectedUserControl { get; set; }
        public static int OpcProcessCount { get; set; } = 0;
        public static int FolderCount { get; internal set; }
        public static int VariableCount { get; internal set; }
    }
}