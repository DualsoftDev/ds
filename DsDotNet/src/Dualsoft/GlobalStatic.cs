using DevExpress.XtraEditors;
using Dual.Common.Core;
using Engine.Core;
using log4net;
using Microsoft.Msagl.GraphmapsWithMesh;
using Microsoft.Win32;
using System;
using System.IO;
using System.Media;
using System.Reactive.Subjects;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Engine.Core.DsType;

namespace DSModeler
{
    public static class K
    {
        public const string AppName = "DSModeler";
        public const string RegSkin = "RegSkin";
        public const string LastPath = "LastPath";
        public const string LastFiles = "LastFiles";

    }
    public static class Global
    {
        public static ILog Logger => Log4NetLogger.Logger;
        public static string LogLevel { get; set; }
        public static int SimSpeed { get; set; } = 0;

        public static Version ver = Assembly.GetEntryAssembly().GetName().Version;

        public static Subject<Tuple<CoreModule.Vertex, Status4>> StatusChangeSubject = new Subject<Tuple<CoreModule.Vertex, Status4>>();

        public static string DefaultFolder =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Dualsoft",
                "Modeler"
            );

        public static string DefaultAppSettingFolder => Path.Combine(DefaultFolder, "AppSetting");
        public static string AppVersion => $"{ver.Major}.{ver.Minor}.{ver.Build}";

    }
    public static class DSRegistry
    {
        static RegistryKey _registryKey => Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Dualsoft\DSModeler");
        public static void SetValue(string key, object value) => _registryKey.SetValue(key, value);
        public static object GetValue(string key) => _registryKey.GetValue(key);
        public static T GetValue<T>(string key) where T : class => _registryKey.GetValue(key) as T;

    }

    
    public static class EmControlTemp
    {

        public static Task DoAsync(this Control control, Action<TaskCompletionSource<object>> action)
        {
            var tcs = new TaskCompletionSource<object>();
             control.BeginInvoke((Action)(() => { action(tcs); }));
            return tcs.Task;
        }

    }


    public static class MBox
    {
        public static DialogResult Error(string text, string caption = "ERROR")
        {
            //Console.Beep();
            //SystemSounds.Hand.Play();
            SystemSounds.Beep.Play();
            return XtraMessageBox.Show(text, caption, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        public static DialogResult AskYesNo(string text, string caption = "ANSWER")
        {
            SystemSounds.Hand.Play();
            return XtraMessageBox.Show(text, caption, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        }
        public static DialogResult WarningAskYesNo(string text, string caption = "ANSWER")
        {
            SystemSounds.Hand.Play();
            return XtraMessageBox.Show(text, caption, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        }
        public static DialogResult AskYesNoCancel(string text, string caption = "ANSWER")
        {
            SystemSounds.Hand.Play();
            return XtraMessageBox.Show(text, caption, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
        }
        public static DialogResult Info(string text, string caption = "INFO")
        {
            SystemSounds.Hand.Play();
            return XtraMessageBox.Show(text, caption, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        public static DialogResult Warn(string text, string caption = "Warning")
        {
            SystemSounds.Beep.Play();
            return XtraMessageBox.Show(text, caption, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}


