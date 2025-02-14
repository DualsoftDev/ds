using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace Dual.Common.Winform
{

    public delegate void UnobservedTaskExceptionEventHandler(object sender, UnobservedTaskExceptionEventArgs e);

    public static class UnhandledExceptionHandler
    {
        /// <summary>
        /// Unobserved Task Exception
        ///  - Task 내부에서 발생한 exception 중, task 내에서 catch 되지 않은 exception.   그냥 두면 발생 여부를 알 수 없다.
        ///  - App.Config 에 runtime section 에 ThrowUnobservedTaskExceptions enabled="true" 를 설정
        ///  - Garbage collect 를 실행
        ///  https://stackoverflow.com/questions/3284137/taskscheduler-unobservedtaskexception-event-handler-never-being-triggered
        /// </summary>
        public static void InstallUnhandledExceptionHandler(UnhandledExceptionEventHandler onUnhandledException, ThreadExceptionEventHandler onThreadedException, EventHandler<UnobservedTaskExceptionEventArgs> onUnobservedTaskException=null)
            //Action<ThreadExceptionEventArgs> onException=null)
        {
            // Add the event handler for handling UI thread exceptions to the event:
            Application.ThreadException += onThreadedException;

            // Set the unhandled exception mode to force all Windows Forms errors to go through our handler:
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            // Add the event handler for handling non-UI thread exceptions to the event:
            AppDomain.CurrentDomain.UnhandledException += onUnhandledException;

            // Task 에서 Exception 이 발생하고, 해당 task 내에서 catch 하지 않아서 숨겨질 수 있는 exception
            // https://stackoverflow.com/questions/3284137/taskscheduler-unobservedtaskexception-event-handler-never-being-triggered
            if (onUnobservedTaskException != null)
                TaskScheduler.UnobservedTaskException += onUnobservedTaskException;
        }

        public static Action<Exception> DefaultActionOnUnhandledException { get; set; }
        public static Action<Exception> DefaultActionOnUnhandledThreadException { get; set; }
        public static Action<Exception> DefaultActionOnUnhandledUnobservedTaskException { get; set; }
        
        static UnhandledExceptionHandler()
        {
            void ShowMessageBox(string message)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    System.Media.SystemSounds.Beep.Play();
                MessageBox.Show(message, "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
            }

            DefaultActionOnUnhandledException =
                new Action<Exception>((ex) => ShowMessageBox($"Unhandled Exception: {ex}"));

            DefaultActionOnUnhandledThreadException =
                new Action<Exception>((ex) => ShowMessageBox($"Threaded unhandled Exception: {ex}"));

            DefaultActionOnUnhandledUnobservedTaskException =
                new Action<Exception>((ex) => ShowMessageBox($"Unobserved task Exception: {ex}"));
        }

        /// <summary>
        /// Unobserved Task Exception
        ///  - Task 내부에서 발생한 exception 중, task 내에서 catch 되지 않은 exception.   그냥 두면 발생 여부를 알 수 없다.
        ///  - App.Config 에 runtime section 에 ThrowUnobservedTaskExceptions enabled="true" 를 설정
        ///  - Garbage collect 를 실행
        ///  https://stackoverflow.com/questions/3284137/taskscheduler-unobservedtaskexception-event-handler-never-being-triggered
        /// </summary>
        public static void InstallUnhandledExceptionHandler()
        {
            UnhandledExceptionEventHandler unhandled =
                (s, e) => DefaultActionOnUnhandledException.Invoke((Exception)e.ExceptionObject);

            ThreadExceptionEventHandler threadedUnhandled =
                (s, e) => DefaultActionOnUnhandledThreadException.Invoke(e.Exception);

            EventHandler<UnobservedTaskExceptionEventArgs> onUnobservedTaskException = (s, e) =>
            {
                DefaultActionOnUnhandledThreadException.Invoke(e.Exception);
                // set observerd 처리 안하면 re-throw 됨
                e.SetObserved();
            };

            InstallUnhandledExceptionHandler(unhandled, threadedUnhandled, onUnobservedTaskException);
        }
    }
}
