using DevExpress.LookAndFeel;
using System;
using System.Windows.Forms;

namespace Dualsoft
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);


            UserLookAndFeel.Default.SetSkinStyle(SkinSvgPalette.Bezier.VSBlue);

            var main = new FormMain();
#if !DEBUG
            SplashScreenManager.ShowForm(main, typeof(SplashScreenDS));
#endif

            Application.Run(main);
        }
    }
}