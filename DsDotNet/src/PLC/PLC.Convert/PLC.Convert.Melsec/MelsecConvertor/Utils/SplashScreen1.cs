using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using DevExpress.XtraSplashScreen;

namespace MelsecConverter
{
    public partial class SplashScreen1 : SplashScreen
    {
        private System.Windows.Forms.Timer timer1 = new Timer();
        int curDll = 0;
        public SplashScreen1()
        {
            InitializeComponent();
            timer1.Tick += Timer1_Tick;
            timer1.Interval = 150;
            timer1.Start();
            var asm = System.Reflection.Assembly.GetEntryAssembly();
            var asmName = asm.GetName();
            labelControl_Ver.Text = string.Format(" v{0} ({1})", asmName.Version
                , System.IO.File.GetLastWriteTime(Assembly.GetExecutingAssembly().Location).ToShortDateString());
        }
        private void Timer1_Tick(object sender, EventArgs e)
        {
            var asmNameDll = Assembly.GetEntryAssembly().GetReferencedAssemblies();
            if (curDll < asmNameDll.Length)
            {
                labelControl_ReferencedAssemblies.Text = $"{asmNameDll[curDll].Name}, Ver{asmNameDll[curDll].Version}";
                curDll++;
            }
        }

        #region Overrides

        public override void ProcessCommand(Enum cmd, object arg)
        {
            base.ProcessCommand(cmd, arg);
        }

        #endregion

        public enum SplashScreenCommand
        {
        }
    }
}