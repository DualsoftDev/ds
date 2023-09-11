using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using Engine.Core;
using static Engine.Core.ModelLoaderModule;
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
            var confFile = Path.ChangeExtension(dsPath, ".json");
            var model = ModelLoader.LoadFromConfig(confFile);

            /////////////////////
            //........
            //........model 이용하여 
            //........HMI 화면 구성
            //........
            //........
            /////////////////////


            this.Show();
        }

        private void barButtonItem_close_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            this.Close();
        }
    }
}