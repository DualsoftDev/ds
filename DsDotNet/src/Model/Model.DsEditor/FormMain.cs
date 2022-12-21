using Model.Import.Office;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

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
            List<string> lstPath = new List<string>();
            lstPath.Add(Path.GetFullPath(Path.Combine(Application.StartupPath, @"..\..\")) + "src\\UnitTest\\UnitTest.Engine\\ImportOffice\\Sample\\Main.pptx");
            lstPath.Add(Path.GetFullPath(Path.Combine(Application.StartupPath, @"..\..\")) + "src\\UnitTest\\UnitTest.Engine\\ImportOffice\\Sample\\SubAssy.pptx");


            var results = ImportM.FromPPTXS(lstPath);
            var model = results.Item1;
            var views = results.Item2;
        }
    }
}
