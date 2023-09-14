using Engine.Core;
using System;
using System.IO;

namespace AppHMI
{
    public partial class MainFormHMI : DevExpress.XtraEditors.XtraForm
    {
        public MainFormHMI()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
        }
        public void LoadHMI(string dsPath)
        {
            string confFile = Path.ChangeExtension(dsPath, ".json");
            _ = ModelLoader.LoadFromConfig(confFile);

            /////////////////////
            //........
            //........model 이용하여 
            //........HMI 화면 구성
            //........
            //........
            /////////////////////


            Show();
        }

        private void barButtonItem_close_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            Close();
        }
    }
}