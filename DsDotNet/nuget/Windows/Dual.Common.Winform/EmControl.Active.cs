using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Dual.Common.Winform
{
    public static partial class EmControl
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr GetAncestor(IntPtr hWnd, uint gaFlags);

        private const uint GA_ROOT = 2;

        /// <summary>
        /// UserControl이 현재 활성 창의 일부인지 여부 반환
        /// </summary>
        public static bool IsPartOfActiveProgram(this Control userControl)
        {
            try
            {
                // UserControl의 최상위 창 핸들 가져오기
                IntPtr userControlHandle = userControl.Handle;
                IntPtr rootHandle = GetAncestor(userControlHandle, GA_ROOT);
                IntPtr foregroundWindow = GetForegroundWindow();

                // 현재 활성 창이 PowerPoint의 활성 창과 같고, UserControl의 최상위 핸들과 동일한지 확인
                return foregroundWindow == rootHandle;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking UserControl activation: {ex.Message}");
                return false;
            }
        }
    }
}
