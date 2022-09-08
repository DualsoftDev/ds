using Engine.Common;
using Model.Import.Office;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
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

            var form = new FormMain();
            Application.Run(form);

        }
    }



}
