using Dual.Common.Core;

using Dual.Common.Core.FS;

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

            // DllVersionChecker.IsValidExDLL(Assembly.GetExecutingAssembly());
            var logger = Log4NetLogger.PrepareLog4Net("ViewerLogger");
            Log4NetWrapper.SetLogger(logger);
            logger.Info("Model import viewer started.");


            //cpu 로딩 사용시 초기화 필요
            Engine.CodeGenCPU.ModuleInitializer.Initialize();

            var form = new FormMain();
            Application.Run(form);

        }
    }



}
