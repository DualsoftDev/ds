using Engine.Common;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Engine.Common.FS;
using static Model.Import.Office.Event;

namespace Dual.Model.Import
{
    public static class UtilFile
    {
        public static string GetNewPath(string pptPath)
        {
            var excelName = Path.GetFileNameWithoutExtension(pptPath) + $"_{DateTime.Now.ToString("yy_MM_dd(HH-mm-ss)")}.xlsx";
            var excelDirectory = Path.Combine(Path.GetDirectoryName(pptPath), Path.GetFileNameWithoutExtension(excelName));
            Directory.CreateDirectory(excelDirectory);
            return Path.Combine(excelDirectory, excelName);
        }


        public static string GetVersion()
        {
            var text = "";

            StreamReader sr = new StreamReader($"{Application.StartupPath}\\last-commit");
            while (sr.Peek() >= 0)
            {
                var txt = sr.ReadLine();
                if (txt.Split('|').Length > 1)
                    text = string.Join(", ", txt.Split('|').Skip(1));
                break;
            }
            sr.Close();

            return text;
        }

        public static bool BusyCheck()
        {
            if (FormMain.TheMain.Busy)
            {
                MessageEvent.MSGWarn("변환 작업중입니다.");
                return true;
            }
            return false;
        }


    }
    public static class RichTextBoxExtensions
    {
        public static void AppendTextColor(this RichTextBox box, string text, Color color)
        {
            FormMain.TheMain.Do(() =>
             {
                 box.SelectionStart = box.TextLength;
                 box.SelectionLength = 0;

                 box.SelectionColor = color;
                 box.AppendText(text);
                 box.SelectionColor = box.ForeColor;
             });

        }
        public static void SetClipboard(string value)
        {
            if (value == null)
                throw new ArgumentNullException("Attempt to set clipboard with null");

            Process clipboardExecutable = new Process();
            clipboardExecutable.StartInfo = new ProcessStartInfo // Creates the process
            {
                RedirectStandardInput = true,
                FileName = @"clip",
                UseShellExecute = false
            };
            clipboardExecutable.Start();

            clipboardExecutable.StandardInput.Write(value); // CLIP uses STDIN as input.
                                                            // When we are done writing all the string, close it so clip doesn't wait and get stuck
            clipboardExecutable.StandardInput.Close();

            return;
        }
    }
    public static class EmException
    {
        /// <summary>
        /// Unobserved Task Exception
        ///  - Task 내부에서 발생한 exception 중, task 내에서 catch 되지 않은 exception.   그냥 두면 발생 여부를 알 수 없다.
        ///  - App.Config 에 runtime section 에 ThrowUnobservedTaskExceptions enabled="true" 를 설정
        ///  - Garbage collect 를 실행
        ///  https://stackoverflow.com/questions/3284137/taskscheduler-unobservedtaskexception-event-handler-never-being-triggered
        /// </summary>
        public static void InstallUnhandledExceptionHandler(UnhandledExceptionEventHandler onUnhandledException, ThreadExceptionEventHandler onThreadedException, EventHandler<UnobservedTaskExceptionEventArgs> onUnobservedTaskException = null)
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


        /// <summary>
        /// Unobserved Task Exception
        ///  - Task 내부에서 발생한 exception 중, task 내에서 catch 되지 않은 exception.   그냥 두면 발생 여부를 알 수 없다.
        ///  - App.Config 에 runtime section 에 ThrowUnobservedTaskExceptions enabled="true" 를 설정
        ///  - Garbage collect 를 실행
        ///  https://stackoverflow.com/questions/3284137/taskscheduler-unobservedtaskexception-event-handler-never-being-triggered
        /// </summary>
        public static void InstallUnhandledExceptionHandler()
        {
            UnhandledExceptionEventHandler unhandled = (s, e) =>
            {
                System.Media.SystemSounds.Beep.Play();
                MessageBox.Show($"Unhandled Exception: {e}", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
            };

            ThreadExceptionEventHandler threadedUnhandled = (s, e) =>
            {
                System.Media.SystemSounds.Beep.Play();
                MessageBox.Show($"Threaded unhandled Exception: {e.Exception}", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
            };

            EventHandler<UnobservedTaskExceptionEventArgs> onUnobservedTaskException = (s, e) =>
            {
                System.Media.SystemSounds.Beep.Play();
                MessageBox.Show($"Unobserved task Exception: {e.Exception}", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
                // set observerd 처리 안하면 re-throw 됨
                e.SetObserved();
            };

            InstallUnhandledExceptionHandler(unhandled, threadedUnhandled, onUnobservedTaskException);
        }
    }


}

