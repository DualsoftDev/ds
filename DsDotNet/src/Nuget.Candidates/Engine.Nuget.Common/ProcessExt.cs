using System.Diagnostics;

namespace Engine.Nuget.Common
{
    public static class ProcessExt
    {
        public static Process? TryFindRunningProcess(string processName) => Process.GetProcesses().FirstOrDefault(p => p.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase));
        public static bool IsRunning(string processName) => TryFindRunningProcess(processName) != null;
        /// <summary>
        /// 해당 process 가 구동중이 아닐 때에만 Process.Start 호출함.
        /// <br/> - 신규 start 시에만 Process 값 반환. 이미 구동중이면 null 반환
        /// </summary>
        public static Process RunSingleton(string processPath)
        {
            var processName = Path.GetFileNameWithoutExtension(processPath);
            if (IsRunning(processName))
                return null;

            var psi = new ProcessStartInfo(processPath)
            {
                UseShellExecute = true,
                //RedirectStandardError = true,
            };

            return Process.Start(psi);
        }
        public static Process RunSingleton(ProcessStartInfo psi)
        {
            if (IsRunning(psi.FileName))
                return null;

            return Process.Start(psi);
        }
        public static Process RestartSingleon(ProcessStartInfo psi)
        {
            var running = TryFindRunningProcess(psi.FileName);
            running?.Kill();
            return RunSingleton(psi);
        }
    }
}
