using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Nuget.Common
{
    public class DcTimer
    {
        /// <summary>
        /// Function 수행 결과와, 수행 시간(ms)을 반환
        /// </summary>
        public static (T result, long duration) Duration<T>(Func<T> func)
        {
            var stopwatch = Stopwatch.StartNew();
            T result = func();
            stopwatch.Stop();
            return (result, stopwatch.ElapsedMilliseconds);
        }
        /// <summary>
        /// Action 수행 시간(ms)을 반환
        /// </summary>
        public static long Duration(Action action)
        {
            var stopwatch = Stopwatch.StartNew();
            action();
            stopwatch.Stop();
            return stopwatch.ElapsedMilliseconds;
        }
    }
}
