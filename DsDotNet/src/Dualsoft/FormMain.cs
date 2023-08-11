using DevExpress.XtraBars;
using DevExpress.XtraBars.Docking;
using DevExpress.XtraBars.Docking2010.Views.WindowsUI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace Dualsoft
{
    public partial class FormMain : DevExpress.XtraBars.FluentDesignSystem.FluentDesignForm
    {
        public FormMain()
        {
            InitializeComponent();
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            CenterToScreen();   
            Text = $"Dualsoft v{Global.AppVersion}";
            LayoutForm.LoadLayout(dockManager);

        }

 

        private void FormMain_Shown(object sender, EventArgs e)
        {
#if !DEBUG
            DevExpress.XtraSplashScreen.SplashScreenManager.CloseForm();
#endif
        }

        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            LayoutForm.SaveLayout(dockManager);
        }
    }
}
