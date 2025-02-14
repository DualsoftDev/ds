using System;
using WinTimer = System.Windows.Forms.Timer;
using SysTimer = System.Timers.Timer;

namespace Dual.Common.Winform
{
    /// <summary>
    /// System.Windows.Forms.Timer : UI 상에서 사용하기 편리 (System.Timers.Timer는 백그라운드에서 사용)
    /// </summary>
    public static class EmWinformTimer
    {
        /// <summary>
        /// Winform Timer를 사용하여 주기적으로 action을 수행한다.
        /// </summary>
        public static void Do(Action<WinTimer> action, int intervalMs)
        {
            WinTimer timer = new WinTimer() { Interval = intervalMs };
            timer.Tick += (s, e) => action(timer);
            timer.Start();
        }

        /// <summary>
        /// Winform Timer를 사용하여 delayMs 이후, 한번만 action을 수행한다.
        /// </summary>
        public static void DoOnce(Action action, int delayMs)
        {
            WinTimer timer = new WinTimer() { Interval = delayMs };
            timer.Tick += (s, e) => { action(); timer.Stop(); timer.Dispose(); };
            timer.Start();
        }
    }

    public static class EmSysTimer
    {
        public static void Do(Action<SysTimer> action, int intervalMs)
        {
            SysTimer timer = new SysTimer() { Interval = intervalMs };
            timer.Elapsed += (s, e) => action(timer);
            timer.Start();
        }
        public static void DoOnce(Action action, int intervalMs)
        {
            SysTimer timer = new SysTimer() { Interval = intervalMs };
            timer.Elapsed += (s, e) => { action(); timer.Stop(); timer.Dispose(); };
            timer.Start();
        }
    }

}
