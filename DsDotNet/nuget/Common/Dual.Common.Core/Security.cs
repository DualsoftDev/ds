using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Versioning;
using System.Security.Principal;

namespace Dual.Common.Core
{
#if NET
    [SupportedOSPlatform("windows")]
#endif
    public static class Security
    {
        /// <summary>
        /// 현재의 assembly 가 관리자 권한으로 실행되고 있는지의 여부를 반환한다.
        /// </summary>
        public static bool IsRunAsAdmin()
        {
            // https://www.codeproject.com/tips/627850/clickonce-deployment-vs-requestedexecutionlevel-eq
            var wi = WindowsIdentity.GetCurrent();
            var wp = new WindowsPrincipal(wi);

            bool runAsAdmin = wp.IsInRole(WindowsBuiltInRole.Administrator);
            return runAsAdmin;
        }

        /// <summary>
        /// 현재의 assembly 를 관리자 권한으로 실행하게 한다.  필요에 따라서 관리자 비밀번호를 입력해야 한다.
        /// 현재의 assembly 를 종료시키고, 다시 구동할 필요가 있는 application ... 예를 들어 자동 update 가 필요한 application 등에서 동일한 방법을 사용할 수 있겠다.
        /// </summary>
        public static void RunAsAdmin()
        {
            if (!IsRunAsAdmin())
            {
                // It is not possible to launch a ClickOnce app as administrator directly,
                // so instead we launch the app as administrator in a new process.
                var processInfo = new ProcessStartInfo(Assembly.GetExecutingAssembly().Location);

                // The following properties run the new process as administrator
                processInfo.UseShellExecute = true;
                processInfo.Verb = "runas";

                // Start the new process
                try
                {
                    System.Console.WriteLine("Running again with administrator privileges");
                    Process.Start(processInfo);
                }
                catch (Exception)
                {
                    // The user did not allow the application to run as administrator
                    System.Console.WriteLine("Sorry, but I don't seem to be able to start " +
                       "this program with administrator rights!");
                }

                // Shut down the current process
                //MediaTypeNames.Application.Exit();
            }

        }
    }
}
