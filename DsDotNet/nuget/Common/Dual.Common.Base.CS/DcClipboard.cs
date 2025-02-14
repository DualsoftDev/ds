using System;
using System.Runtime.InteropServices;

namespace Dual.Common.Base.CS
{
    /// <summary>
    /// System.WIndows.Form 없이 clipboard 기능 구현.  DcClipboard.Read(), DcClipboard.Write()
    /// </summary>
    public static class DcClipboard
    {
        [DllImport("user32.dll")] internal static extern bool OpenClipboard(IntPtr hWndNewOwner);
        [DllImport("user32.dll")] internal static extern bool CloseClipboard();
        [DllImport("user32.dll")] internal static extern bool EmptyClipboard();
        [DllImport("user32.dll")] internal static extern IntPtr SetClipboardData(uint uFormat, IntPtr data);
        [DllImport("user32.dll")] internal static extern IntPtr GetClipboardData(uint uFormat);
        [DllImport("user32.dll")] internal static extern bool IsClipboardFormatAvailable(uint format);
        [DllImport("kernel32.dll", SetLastError = true)] internal static extern IntPtr GlobalLock(IntPtr hMem);
        [DllImport("kernel32.dll", SetLastError = true)] internal static extern bool GlobalUnlock(IntPtr hMem);
        [DllImport("kernel32.dll", SetLastError = true)] internal static extern int GlobalSize(IntPtr hMem);

        private const uint CF_UNICODETEXT = 13;
        private const uint GMEM_MOVEABLE = 0x0002;

        public static void WriteText(string text)
        {
            if (string.IsNullOrEmpty(text))
                throw new ArgumentNullException(nameof(text));

            OpenClipboard(IntPtr.Zero);
            try
            {
                EmptyClipboard();

                var hGlobal = Marshal.StringToHGlobalUni(text);
                if (SetClipboardData(CF_UNICODETEXT, hGlobal) == IntPtr.Zero)
                    throw new SystemException("Failed to set clipboard data.");
            }
            finally
            {
                CloseClipboard();
            }
        }

        public static string ReadText()
        {
            if (!IsClipboardFormatAvailable(CF_UNICODETEXT))
                throw new InvalidOperationException("No Unicode text available in clipboard.");

            OpenClipboard(IntPtr.Zero);
            try
            {
                var handle = GetClipboardData(CF_UNICODETEXT);
                if (handle == IntPtr.Zero)
                    throw new SystemException("Failed to get clipboard data.");

                var pointer = GlobalLock(handle);
                if (pointer == IntPtr.Zero)
                    throw new SystemException("Failed to lock global memory.");

                try
                {
                    int size = GlobalSize(handle);
                    return Marshal.PtrToStringUni(pointer, size / 2).TrimEnd('\0');
                }
                finally
                {
                    GlobalUnlock(handle);
                }
            }
            finally
            {
                CloseClipboard();
            }
        }
        public static void Write(string text) => WriteText(text);
        public static string Read() => ReadText();

    }
}
