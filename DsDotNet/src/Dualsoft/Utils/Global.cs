using log4net;
using Microsoft.Win32;
using System;
using System.IO;
using System.Reflection;

namespace Dualsoft
{
    public static class Global
    {
        public static ILog Logger { get; set; }
        public static Version ver = Assembly.GetEntryAssembly().GetName().Version;

        public static string DefaultFolder =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Dualsoft"
            );

        public static string DefaultAppSettingFolder => Path.Combine(DefaultFolder, "AppSetting");
        public static string AppVersion => $"{ver.Major}.{ver.Minor}.{ver.Build}";

    }
    public static class DSRegistry
    {
        static RegistryKey _registryKey => Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Dualsoft\AppDotNet");
        public static void SetValue(string key, object value) => _registryKey.SetValue(key, value);
        public static object GetValue(string key) => _registryKey.GetValue(key);
        public static T GetValue<T>(string key) where T : class => _registryKey.GetValue(key) as T;

    }

    public static class K
    {
        public const string RegSkin = "RegSkin";
       

    }
}


