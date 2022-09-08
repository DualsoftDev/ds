using Model.Import.Office;
using System;
using System.Diagnostics;
using System.Drawing;
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
            InstallUnhandledExceptionHandler();
            var form = new FormMain();
            Application.Run(form);

            void InstallUnhandledExceptionHandler()
            {
                UnhandledExceptionEventHandler unhandled = (s, e) =>
                {
                    System.Media.SystemSounds.Beep.Play();
                    Event.MSGError($"{e.ExceptionObject}");
                    var msg = $"XXX Unhandled Exception: {e} {e.ExceptionObject}";
                    Console.WriteLine(msg);
                };

                ThreadExceptionEventHandler threadedUnhandled = (s, e) =>
                {
                    System.Media.SystemSounds.Beep.Play();
                    Event.MSGError($"{e.Exception.Message}");
                    var msg = $"Threaded unhandled Exception: {e.Exception}";
                    Console.WriteLine(msg);
                };

                EventHandler<UnobservedTaskExceptionEventArgs> onUnobservedTaskException = (s, e) =>
                {
                    System.Media.SystemSounds.Beep.Play();
                    Event.MSGError($"{e.Exception.Message}");
                    var msg = $"Unobserved task Exception: {e.Exception}";
                    Console.WriteLine(msg);

                    // set observerd 처리 안하면 re-throw 됨
                    e.SetObserved();
                };

                EmException.InstallUnhandledExceptionHandler(unhandled, threadedUnhandled, onUnobservedTaskException);
            }
        }
    }

    

}
