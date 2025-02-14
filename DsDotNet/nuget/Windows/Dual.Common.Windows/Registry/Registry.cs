using System;
using System.Collections.Generic;
using System.Linq;

using log4net;

using Microsoft.Win32;

using Newtonsoft.Json;

namespace Dual.Common.Windows
{
    public class DcRegistry
    {
        public static ILog Logger { get; set; }

        /// <summary>
        /// progId 의 COM 등록 full path 를 반환. (HLCR/CLSID/{xxxxxxxx-xx...}/LocalServer32/ 하위의 path 정보)
        /// </summary>
        /// <param name="progId"></param>
        /// <returns></returns>
        public static string GetRegisteredPathFromProgId(string progId)
        {
            var guid = Type.GetTypeFromProgID(progId).GUID;
            RegistryKey key = Registry.ClassesRoot.OpenSubKey(@"CLSID\" + guid.ToString("B"));

            /* 실패하면 32-bit 로 registry 등록되었는지도 검사한다. */
            if ( key == null )
                key = Registry.ClassesRoot.OpenSubKey(@"Wow6432Node\CLSID\" + guid.ToString("B"));

            if ( key == null )
                throw new Exception($"Failed to find registry key for progId {progId}");

            var subKey = key.OpenSubKey("LocalServer32");
            string path = (string)subKey.GetValue("");
            if (path.StartsWith("\"") && path.EndsWith("\""))
                path = path.Replace("\"", "");

            return path;
        }


        private static Dictionary<string, RegistryKey> _registryKeys = new Dictionary<string, RegistryKey>()
        {
            {"HKCR", Registry.ClassesRoot},
            {"HKCU", Registry.CurrentUser},
            {"HKLM", Registry.LocalMachine},
        };

        /// <summary>
        ///
        /// </summary>
        /// <param name="subKey">e.g @"Software\Dualsoft\DualsoftApp"</param>
        /// <param name="baseKeyName"></param>
        /// <param name="createOnDemand"></param>
        /// <param name="openWritable"></param>
        /// <returns></returns>
        /// todo : check Application.UserAppDataRegistry or Application.CommonAppDataRegistry
        ///
        public static RegistryKey OpenSubKey(string mainKey, string subKey, string baseKeyName ="HKCU", bool createOnDemand=true, bool openWritable=true)
        {
            RegistryKey baseKey = _registryKeys[baseKeyName];
            var key = $@"{mainKey}\{subKey}";
            RegistryKey rKey = baseKey.OpenSubKey(key, openWritable);
            if ( rKey == null && createOnDemand )
                rKey = baseKey.CreateSubKey(key);

            return rKey;
        }

        /*
         * Sample Usage

            RegistryKey key = DcRegistry.OpenSubKey(_application.RegistryLocation);
            var obj = key.GetValue("ConversionFolder");
            string dir = obj == null ? "" : obj.ToString();
            ...

            key.SetValue("ConversionFolder", Path.GetDirectoryName("SomeNewValues..."));
        }
         */


        public static T Get<T>(string mainKey, string subKey, string baseKeyName="HKCU", bool createOnDemand=true, bool openWritable=true) where T : class
        {
            RegistryKey key = DcRegistry.OpenSubKey(mainKey, subKey, baseKeyName, createOnDemand, openWritable);
            var obj = key?.GetValue(subKey);
            if (obj == null)
                return null;
            return JsonConvert.DeserializeObject<T>(obj.ToString());
        }

        public static void Set<T>(T obj, string mainKey, string subKey, string baseKeyName = "HKCU", bool createOnDemand = true, bool openWritable = true)
        {
            RegistryKey key = DcRegistry.OpenSubKey(mainKey, subKey, baseKeyName, createOnDemand, openWritable);
            var json = JsonConvert.SerializeObject(obj);
            key.SetValue(subKey, json);
        }


        public static bool DeleteSubKey(string subKey, string baseKeyName = "HKCU")
        {
            RegistryKey baseKey = _registryKeys[baseKeyName];
            RegistryKey key = baseKey.OpenSubKey(subKey, writable:true);

            try
            {
                key.DeleteSubKeyTree(subKey);
                return true;
            }
            catch (Exception ex)
            {
                Logger?.Error($"ERROR: {ex}");
                return false;
            }
        }

        public static bool DeleteKey(string keyLocation, string baseKeyName = "HKCU")
        {
            RegistryKey baseKey = _registryKeys[baseKeyName];
            var keyTokens = keyLocation.Split(new []{'\\'}, StringSplitOptions.RemoveEmptyEntries);
            var keyParentPath = String.Join("\\", keyTokens.Take(keyTokens.Length - 1));
            var keyPath = String.Join("\\", keyTokens);
            var keyName = keyTokens.Last();

            try
            {
                using (RegistryKey parentKey = baseKey.OpenSubKey(keyParentPath, writable: true))
                using (RegistryKey key = baseKey.OpenSubKey(keyPath, writable: true))
                {
                    if (parentKey != null && key != null)
                    {
                        parentKey.DeleteSubKeyTree(keyName);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger?.Error($"ERROR: {ex}");
            }

            return false;
        }
    }
}
