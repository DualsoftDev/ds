using System;
using System.Diagnostics;
using System.Security.Permissions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dsu.Common.Utilities.Exceptions;

namespace Dsu.Common.Utilities
{
    public static class ToolsDateTime
    {
        public static DateTime ChangeTimePart(DateTime dtBase, int h, int m) { return ChangeTimePart(dtBase, h, m, 0, 0); }
        public static DateTime ChangeTimePart(DateTime dtBase, int h, int m, int s/*=0*/, int ms/*=0*/)
        {
            return new DateTime(dtBase.Year, dtBase.Month, dtBase.Day, h, m, s, ms);
        }

        public static DateTime ChangeDatePart(DateTime dtBase, int y, int m, int d)
        {
            return new DateTime(y, m, d, dtBase.Hour, dtBase.Minute, dtBase.Second, dtBase.Millisecond);
        }


        public static async Task<bool> TimeBoundCheckExecutorAsync(this Func<bool> checkAction, int limitMilli, int sleepMilli = 100)
        {
            Stopwatch s = new Stopwatch();
            s.Start();
            while ( s.Elapsed < TimeSpan.FromMilliseconds(limitMilli))
            {
                if (checkAction())
                    return true;

                await Task.Delay(sleepMilli);
            }
            s.Stop();

            return false;
        }

        /// <summary>
        /// action 을 수행한 후, 주어진 시간(limitMilli) 동안 종료되지 않으면 해당 action abort 하여 deadlock 방지.
        /// </summary>
        /// <param name="timeTakingAction">시간을 요하는 action</param>
        /// <param name="mainThreadControl"></param>
        /// <param name="limitMilli">action 종료를 기다리는 한정된 시간</param>
        /// <param name="sleepMilli">check interval</param>
        /// <returns>정산 수행 여부 반환</returns>
        /// 
        /// http://stackoverflow.com/questions/1327102/how-to-kill-a-thread-instantly-in-c
        /// 
        [Obsolete("Use ExecuteWithTimeLimit, instead.")]
        [SecurityPermission(SecurityAction.Demand, ControlThread = true)]
        public static bool TimeBoundActionExecutor(this Action timeTakingAction, Control mainThreadControl, int limitMilli, int sleepMilli = 100)
        {
            Stopwatch s = new Stopwatch();
            s.Start();
            bool finished = false;
            var thread = ExceptionSafeThreadStarter.Start(null, mainThreadControl, () =>
            {
                timeTakingAction();
                finished = true;
            });

            while (s.Elapsed < TimeSpan.FromMilliseconds(limitMilli))
            {
                if (finished)
                    return true;

                Thread.Sleep(sleepMilli);
            }
            s.Stop();
            thread.Abort();

            return false;
        }


        /// <summary>
        /// http://stackoverflow.com/questions/7413612/how-to-limit-the-execution-time-of-a-function-in-c-sharp
        /// F# 구현은 Dsu.Common.Utilities.FS 의 Functions.withTimeLimit 참고
        /// </summary>
        /// <param name="timeTakingAction"></param>
        /// <param name="timeSpan"></param>
        /// <param name="cancelOnExpire"></param>
        /// <returns></returns>
        public static bool ExecuteWithTimeLimit(this Action timeTakingAction, TimeSpan timeSpan, bool cancelOnExpire=true)
        {
            return ExecuteWithTimeLimit(timeTakingAction, timeSpan.Milliseconds, cancelOnExpire);
        }
        public static bool ExecuteWithTimeLimit(this Action timeTakingAction, int limitMilli, bool cancelOnExpire=true)
        {
            Task task = null;
            if (cancelOnExpire)
            {
                CancellationTokenSource cts = new CancellationTokenSource();
                cts.CancelAfter(limitMilli);
                task = Task.Run(() => timeTakingAction(), cts.Token);
                task.Wait(limitMilli + 1);
            }
            else
            {
                task = Task.Run(() => timeTakingAction());
                task.Wait(limitMilli);
            }

            return task.IsCompleted;
        }        
    }
}