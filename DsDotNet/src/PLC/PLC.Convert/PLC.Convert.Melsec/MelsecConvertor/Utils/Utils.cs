using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace MelsecConverter
{
    public static class Utils
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        /// <summary>
        /// Windows Explorer 창을 찾고, 지정된 폴더 경로가 열려 있는 경우 해당 창으로 포커스를 이동합니다.
        /// </summary>
        /// <param name="folderPath">포커스를 맞추고자 하는 폴더 경로</param>
        /// <returns>포커스를 이동했다면 true, 그렇지 않으면 false</returns>
        public static bool FocusIfExplorerOpen(string folderPath)
        {
            bool found = false;

            EnumWindows((hWnd, lParam) =>
            {
                if (!IsWindowVisible(hWnd)) return true;

                // 프로세스 ID 가져오기
                GetWindowThreadProcessId(hWnd, out uint processId);
                var process = Process.GetProcessById((int)processId);

                if (process.ProcessName.Equals("explorer", StringComparison.OrdinalIgnoreCase))
                {
                    // 창의 전체 경로 가져오기
                    string windowPath = GetExplorerWindowPath(process);

                    if (!string.IsNullOrEmpty(windowPath) &&
                        string.Equals(windowPath, folderPath, StringComparison.OrdinalIgnoreCase))
                    {
                        SetForegroundWindow(hWnd);
                        found = true;
                        return false; // 탐색 중지
                    }
                }

                return true; // 계속 탐색
            }, IntPtr.Zero);

            return found;
        }

        /// <summary>
        /// Windows Explorer 프로세스를 기반으로 탐색기 창의 전체 경로를 가져옵니다.
        /// </summary>
        /// <param name="process">Explorer 프로세스</param>
        /// <returns>탐색기 창의 전체 경로</returns>
        private static string GetExplorerWindowPath(Process process)
        {
            try
            {
                if (process.MainWindowHandle == IntPtr.Zero) return null;

                // COM 인터페이스를 통해 현재 경로를 가져옵니다.
                dynamic shellWindow = Activator.CreateInstance(Type.GetTypeFromProgID("Shell.Application"));
                var windows = shellWindow.Windows();

                foreach (var window in windows)
                {
                    if (window.HWND == (long)process.MainWindowHandle)
                    {
                        return window.Document.Folder.Self.Path;
                    }
                }
            }
            catch
            {
                // 예외가 발생하면 null 반환
            }

            return null;
        }

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    }
}
