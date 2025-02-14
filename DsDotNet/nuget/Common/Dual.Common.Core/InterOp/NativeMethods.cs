using System;
using System.Runtime.InteropServices;

namespace Dual.Common.Core
{
    public class NativeMethods
    {
        // http://ecomnet.tistory.com/41
        // http://www.codeproject.com/Articles/27298/Dynamic-Invoke-C-DLL-function-in-C

        [DllImport("kernel32.dll")]
        public static extern int LoadLibrary([MarshalAs(UnmanagedType.LPStr)] string lpLibFileName);
        [DllImport("kernel32.dll")]
        public static extern IntPtr GetProcAddress(int hModule, [MarshalAs(UnmanagedType.LPStr)] string lpProcName);
        [DllImport("kernel32.dll")]
        public static extern bool FreeLibrary(int hModule);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void Ftn_Void_Void();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int Ftn_Int_Void();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int Ftn_Int_String(string args);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate string Ftn_String_String(string args);


        #region Console attach/detach
        // http://www.codeproject.com/Tips/68979/Attaching-a-Console-to-a-WinForms-application
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int FreeConsole();

        private void ShowConsoleAttachmentSample()
        {
            NativeMethods.AllocConsole();
            System.Console.WriteLine("Debug Console");
            //Console.ReadKey();

        }
        #endregion

        internal struct SystemTime
        {
            public ushort Year;
            public ushort Month;
            public ushort DayOfWeek;
            public ushort Day;
            public ushort Hour;
            public ushort Minute;
            public ushort Second;
            public ushort Millisecond;
        };

        [DllImport("kernel32.dll", EntryPoint = "SetLocalTime", SetLastError = true)]
        internal static extern bool Win32SetLocalTime(ref SystemTime st);

        /// 컴퓨터의 시간을 변경 (local time).  SetSystemTime 는 time zone 을 고려해야 한다.
        public static bool SetLocalTime(DateTime dt)
        {
            var systm = new SystemTime()
            {
                Year = (ushort)dt.Year,
                Month = (ushort)dt.Month,
                DayOfWeek = (ushort)dt.DayOfWeek,
                Day = (ushort)dt.Day,
                Hour = (ushort)dt.Hour,
                Minute = (ushort)dt.Minute,
                Second = (ushort)dt.Second,
                Millisecond = (ushort)dt.Millisecond
            };

            return Win32SetLocalTime(ref systm);
        }

        // http://bytes.com/topic/c-sharp/answers/229029-p-invoke-returning-passing-bools-between-c-c
        [DllImport("my.dll")]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool SomeFunctionReturningBool();

        [DllImport("my.dll")][return: MarshalAs(UnmanagedType.I1)]
        private static extern bool SomeFunctionReturningBool2();


        public void ShowSample()
        {
            //int hModule = LoadLibrary(@"V:\WorkingSVN\PLCStudio\trunk\bin\Gp.dll");
            //IntPtr proc = GetProcAddress(hModule, "CheckDummyCall");
            //Ftn_String_String ftn = (Ftn_String_String)Marshal.GetDelegateForFunctionPointer(proc, typeof(Ftn_String_String));
            //string result = ftn("test function");
        }

        /// Convert GetLastWin32Error() result
        public static string LastWin32ErrorToString() => LastWin32ErrorToString(Marshal.GetLastWin32Error());
        public static string LastWin32ErrorToString(int error) => new System.ComponentModel.Win32Exception(error).Message;
    }
}
