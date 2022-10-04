using Engine.Common;
using System;
using System.Reflection;
using System.Windows.Forms;

namespace Dual.Model.Import
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
            SimpleExceptionHandler.InstallExceptionHandler();

            DllVersionChecker.IsValidExDLL(Assembly.GetExecutingAssembly());
            //var logger = Log4NetHelper.PrepareLog4Net("ModelImport");
            //Log4NetWrapper.SetLogger(logger);
            //Global.Logger = logger;

            var form = new FormMain();
            Application.Run(form);

        }
    }



}
