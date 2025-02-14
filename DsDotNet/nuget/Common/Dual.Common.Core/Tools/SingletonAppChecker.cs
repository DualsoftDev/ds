using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Dual.Common.Core
{
    /// <summary>
    /// 실행 프로그램의 중복 실행 여부를 검사한다.
    /// </summary>
    public static class SingletonAppChecker
    {
        static string _appName = System.Reflection.Assembly.GetEntryAssembly().ManifestModule.Name.Replace(".exe", "").Replace(".dll", "");
        // https://stackoverflow.com/questions/819773/run-single-instance-of-an-application-using-mutex
        /// <summary>
        /// Mutex 기반으로, 프로그램의 중복 실행 여부를 판별한다.
        /// </summary>
        public static bool IsOK()
        {
            var createdNew = false;
            var m = new Mutex(false, _appName, out createdNew);
            return createdNew;
        }

        /// <summary>
        /// 구동중인 process 정보를 기반으로, 프로그램의 중복 실행 여부를 판별한다.
        /// </summary>
        public static bool IsOKByProcessInspection()
        {
            var myProc = Process.GetCurrentProcess();
            var others = Process.GetProcessesByName(_appName).ToArray();
            return others.Length == 1 && others[0].Id == myProc.Id;
        }
    }
}
