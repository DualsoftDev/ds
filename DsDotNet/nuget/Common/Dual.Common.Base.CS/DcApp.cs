using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Runtime.Versioning;

namespace Dual.Common.Base.CS
{
    public static class DcApp
    {
        static public void Initialize()
        {
            var assembly = Assembly.GetEntryAssembly();
            if (assembly != null)
            {
                var attributes = assembly.GetCustomAttributes(typeof(DebuggableAttribute), false);
                if (attributes.Length > 0)
                {
                    var debuggableAttribute = (DebuggableAttribute)attributes[0];
                    if (debuggableAttribute.IsJITTrackingEnabled
                        || (debuggableAttribute.DebuggingFlags & DebuggableAttribute.DebuggingModes.Default) != 0)
                    {
                        // 잘 맞지 않는 경우가 많음!
                        IsDebugVersion = true;
                    }
                }
            }
        }



        /// <summary>
        /// DEBUG flag 설정 여부.  '#if DEBUG' 로 설정할 수 없다.   call site 의 DEBUG 설정 여부에 의해서 결정되어야 한다.
        /// </summary>
        public static bool IsDebugVersion { get; private set; } = false;

        public static bool IsInUnitTest() =>
            AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.FullName)
                .Any(n => n.StartsWith("Microsoft.VisualStudio.TestPlatform."))
                ;

        /// <summary>
        /// 관리자 권한으로 실행 중인지 여부
        /// </summary>
        //[SupportedOSPlatform("windows")]
        public static bool IsAdministrator()
        {
#pragma warning disable CA1416
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
#pragma warning restore CA1416
        }
    }
}
