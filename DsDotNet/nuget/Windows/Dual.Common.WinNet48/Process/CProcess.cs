using System;
using System.Diagnostics;
using System.IO;
using Dsu.Common.Utilities.Exceptions;
using Dsu.Common.Utilities.ExtensionMethods;

namespace Dsu.Common.Utilities
{
    public class CProcess
    {
        /// <summary>
        /// 프로세스를 kill 한다.
        /// </summary>
        /// <param name="appName">Process name : ".exe" 제외</param>
        static public void KillProcess(string appName)
        {
            Process[] procs = Process.GetProcessesByName(appName);
            foreach (Process p in procs)
                p.Kill();
        }

        static public void StartProcess(string appName)
        {
            Process.Start(appName);
        }


        static public void KillProcesses(params string[] astrProcessName)
        {
            foreach (string proc in astrProcessName)
                KillProcess(proc);
        }

        public static Process FindRunningProcess(string pathServerExe)
        {
            foreach (var process in Process.GetProcesses())
            {
                try
                {
                    if (process.ProcessName.ToLower().IsOneOf("system", "idle", "nosstarter.npe", "nossvc", "svchost", "searchprotocolhost", "searchfilterhost", "audiodg") /*|| ! process.ProcessName.EndsWith(".exe")*/ )
                        continue;

                    var procPath = Path.GetFullPath(process.Modules[0].FileName);
                    if (procPath.Equals(pathServerExe))
                        return process;

                    var originalName = process.Modules[0].FileVersionInfo.OriginalFilename;
                    if (process.Modules[0].ModuleName != originalName)
                    {
                        var dir = Path.GetDirectoryName(procPath);
                        procPath = Path.Combine(dir, originalName);
                        if (procPath.Equals(pathServerExe))
                            return process;
                    }
                }
                catch (Exception ex)
                {
                    ExceptionHider.SwallowException(ex, String.Format("FindRunningProcess({0})", process.ProcessName));
                }
            }

            return null;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="progId"> i.e "LicenseChecker.LicenseChecker" </param>
        /// <returns></returns>
        static public object GetApplicationInstanceFromProgId(string progId)
        {
            try
            {
                // Calling a COM Component From C# (Late Binding)
                // http://www.c-sharpcorner.com/UploadFile/psingh/CallingCOMComponentFromCSharp12022005231615PM/CallingCOMComponentFromCSharp.aspx
                Type tApplication = System.Type.GetTypeFromProgID(progId);
                if (tApplication == null)
                {
                    Tools.ShowMessage("Failed to get type of {0}!!!\r\nCheck proper registration.", progId);
                    return null;
                }

                // TIPS : C# : Creating COM instance
                object oApplication = System.Activator.CreateInstance(tApplication);
                if (oApplication == null)
                {
                    Tools.ShowMessage("Failed to create instance of LicenseChecker.LicenseChecker!!!");
                    return null;
                }

                return oApplication;
            }
            catch (Exception e)
            {
                Tools.ShowMessageOnce("Exception on GetInstance({0}) :\r\n{1}", progId, e.Message);
                return null;
            }
        }
    }
}
