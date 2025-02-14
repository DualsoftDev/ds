using System;
using System.Runtime.Versioning;

using Microsoft.Win32;

namespace Dual.Common.Core.InterOp
{
    // e.g Dual.Common.Core.InterOp.ComSurrogateRegistry.RegisterComClass("{AFEA5515-AE9C-11D3-83AE-00A024BDBF2B}") |> ignore
#if NET
    [SupportedOSPlatform("windows")]
#endif
    public static class ComSurrogateRegistry
    {
        private static bool RegisterDll(string clsid)
        {
            const bool openReadWrite = true;
            var key = Registry.ClassesRoot.OpenSubKey(@"Wow6432Node\CLSID\" + clsid, openReadWrite);
            if (key == null)
                return false;

            var value = key.GetValue("AppID");
            if (value == null)
                key.SetValue("AppID", clsid);


            key = Registry.ClassesRoot.OpenSubKey(@"Wow6432Node\AppID\" + clsid, openReadWrite);
            if (key == null)
            {
                key = Registry.ClassesRoot.CreateSubKey(@"Wow6432Node\AppID\" + clsid);
                key.SetValue("DllSurrogate", "");
            }

            return true;
        }

        public static bool RegisterComClass(string clsid)
        {
            /*
             * 32-bit 는 surrogate 설치 필요 없음.
             */
            if (!Environment.Is64BitProcess)
                return false;
            return RegisterDll(clsid);
        }

    }
}
