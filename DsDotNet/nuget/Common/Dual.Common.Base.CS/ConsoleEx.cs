using System;
using System.Diagnostics;
using System.IO;
using System.Reactive.Disposables;
using System.Runtime.InteropServices;

namespace Dual.Common.Base.CS
{
    public static class ConsoleEx
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();

        [DllImport("kernel32.dll")] static extern IntPtr GetConsoleWindow();
        [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        const int SW_HIDE = 0;
        const int SW_SHOW = 5;

        [DllImport("kernel32.dll", SetLastError = true)] static extern bool AttachConsole(uint dwProcessId);
        private const uint ATTACH_PARENT_PROCESS = 0xFFFFFFFF;

        /// <summary>
        /// console 이 없는 Winform App 에서 새로운 console 창을 생성한다.
        /// </summary>
        public static bool Allocate(string title)
        {
            if (!AllocConsole())
                return false;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Console.SetBufferSize(Console.BufferWidth, 3000);

            var pid = Process.GetCurrentProcess().Id;
            Console.Title = $"{title}: {pid}";
            return true;
        }

        /// <summary>
        /// 이미 존재하는 console 에서 winform.exe 를 실행하고, 해당 winform 에서 존재하는 console 에 attach 하려는 경우.
        /// <br/> - 탐색기 등에서 double click 으로 실행하는 경우, 기존 console 이 없으므로 false 를 반환한다.
        /// <br/>   - 이때는 Allocate() 를 사용해서 새로운 console 을 생성해야 한다.
        /// </summary>
        public static bool Attach()
        {
            if (! AttachConsole(ATTACH_PARENT_PROCESS))
                return false;

            StreamWriter standardOutput = new StreamWriter(Console.OpenStandardOutput())　{　AutoFlush = true　};
            Console.SetOut(standardOutput);

            StreamWriter standardError = new StreamWriter(Console.OpenStandardError()) { AutoFlush = true };
            Console.SetError(standardError);

            return true;
        }

        public static void Show() => ShowWindow(GetConsoleWindow(), SW_SHOW);
        public static void Hide() => ShowWindow(GetConsoleWindow(), SW_HIDE);

        public static IDisposable ConsoleColorChanger(ConsoleColor color)
        {
            var backup = Console.ForegroundColor;
            Console.ForegroundColor = color;
            return Disposable.Create(() => Console.ForegroundColor = backup);
        }
        public static void WriteWithColor(ConsoleColor color, string message)
        {
            using (ConsoleColorChanger(color))
                Console.Write(message);
        }
        public static void WriteLineWithColor(ConsoleColor color, string message)
        {
            using (ConsoleColorChanger(color))
                Console.WriteLine(message);
        }

        public static void Error(string message)
        {
            using (ConsoleColorChanger(ConsoleColor.Red))
                Console.Error.WriteLine(message);
        }
        public static void Warn(string message)
        {
            using (ConsoleColorChanger(ConsoleColor.Yellow))
                Console.WriteLine(message);
        }
        public static void Debug(string message)
        {
            using (ConsoleColorChanger(ConsoleColor.Gray))
                Console.WriteLine(message);
        }
    }
}
