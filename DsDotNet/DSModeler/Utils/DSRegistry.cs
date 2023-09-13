using Microsoft.Win32;

namespace DSModeler.Utils
{
    [SupportedOSPlatform("windows")]

    public static class RegKey
    {
        public const string RegSkin = "RegSkin";
        public const string LastPath = "LastPath";
        public const string LastFiles = "LastFiles";
        public const string LastDocs = "LastDocs";
        public const string SimSpeed = "SimSpeed";
        public const string LayoutMenuExpand = "LayoutMenuExpand";
        public const string LayoutGraphLineType = "LayoutGraphLineType";
        public const string CpuRunMode = "CpuRunMode";
        public const string RunCountIn = "RunCountIn";
        public const string RunCountOut = "RunCountOut";
        public const string RunHWIP = "RunHWIP";
        public const string RunHWDevice = "RunHWDevice";
    }
    public static class DSRegistry
    {
        public const string RegPath = "SOFTWARE\\Dualsoft\\DSModeler";

        private static RegistryKey _registryKey => Registry.CurrentUser.CreateSubKey($@"{RegPath}");
        public static void SetValue(string key, object value)
        {
            _registryKey.SetValue(key, value);
        }

        public static object GetValue(string key)
        {
            return _registryKey.GetValue(key);
        }

        public static T GetValue<T>(string key) where T : class
        {
            return _registryKey.GetValue(key) as T;
        }
    }
}


