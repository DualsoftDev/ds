using Microsoft.Win32;
using System.Runtime.Versioning;

namespace DSModeler
{
    [SupportedOSPlatform("windows")]
    public static class DSRegistry
    {
        static RegistryKey _registryKey => Registry.CurrentUser.CreateSubKey($@"{K.RegPath}");
        public static void SetValue(string key, object value) => _registryKey.SetValue(key, value);
        public static object GetValue(string key) => _registryKey.GetValue(key);
        public static T GetValue<T>(string key) where T : class => _registryKey.GetValue(key) as T;

    }
}


