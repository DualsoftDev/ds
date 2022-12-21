using System;
using System.Reflection;
using System.Windows.Forms;

namespace Model.DsEditor
{
    internal static class Program
    {
        /// <summary>
        /// 해당 애플리케이션의 주 진입점입니다.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Engine.CodeGenCPU.ModuleInitializer.Initialize();


            var form = new FormMain();
            Application.Run(form);

        }
    }



}
