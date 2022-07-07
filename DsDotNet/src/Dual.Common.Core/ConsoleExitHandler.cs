using System;
using System.Runtime.InteropServices;

namespace Dual.Common.Core
{
    // https://www.codegrepper.com/code-examples/csharp/detect+console+close+C%23
    public static class ConsoleExitHandler
    {
        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(SetConsoleCtrlEventHandler handler, bool add);
        public delegate bool SetConsoleCtrlEventHandler(CtrlType sig);

        public static bool SetConsoleControlHandler(SetConsoleCtrlEventHandler handler, bool add) => SetConsoleCtrlHandler(handler, add);

        public enum CtrlType
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }

#if SHOW_SAMPLE
        static bool SampleHandler(CtrlType signal)
        {
            switch (signal)
            {
                case CtrlType.CTRL_BREAK_EVENT:
                case CtrlType.CTRL_C_EVENT:
                case CtrlType.CTRL_LOGOFF_EVENT:
                case CtrlType.CTRL_SHUTDOWN_EVENT:
                case CtrlType.CTRL_CLOSE_EVENT:

                    // CODE HERE WHEN CLOSED
                    try
                    {
                        Console.WriteLine($"Got control signal {signal}");
                    }
                    catch (Exception ex) {
                        Console.WriteLine($"Excetion while executing registered action: {ex}");
                    }

                    Environment.Exit(0);
                    return false;

                default:
                    return false;
            }
        }


        // In your code
        // SetConsoleCtrlHandler(SampleHandler, true); // Register the handle
#endif
    }
}
