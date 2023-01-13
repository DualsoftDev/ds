using Engine.Common;
using Model.Import.Office;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using static Model.Import.Office.ImportM;

namespace Model.DsEditor
{
    public partial class FormMain : Form
    {
        public FormMain()
        {
            InitializeComponent();
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            //List<string> lstPath = new List<string>();
            //lstPath.Add(Path.GetFullPath(Path.Combine(Application.StartupPath, @"..\..\")) + "src\\UnitTest\\UnitTest.Engine\\ImportOffice\\Sample\\Main.pptx");
            //lstPath.Add(Path.GetFullPath(Path.Combine(Application.StartupPath, @"..\..\")) + "src\\UnitTest\\UnitTest.Engine\\ImportOffice\\Sample\\SubAssy.pptx");

            //var results = ImportPPT.GetDsFilesWithLib(lstPath);

            //richTextBox_Debug.Clear();
            //results.ForEach(f => richTextBox_Debug.AppendText(f.Item2 + "\t" + f.Item3 + "\r\n"));
            //richTextBox_Debug.AppendText("\r\n\r\n\r\n");
            //results.ForEach(f => richTextBox_Debug.AppendText(f.Item2 + "\t" + f.Item3 + "\r\n" + f.Item1 + "\r\n"));

        }
    }
}