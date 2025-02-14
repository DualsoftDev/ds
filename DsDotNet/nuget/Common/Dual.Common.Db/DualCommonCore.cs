using System;
using System.Linq;

namespace Dual.Common.Db
{
    /// <summary>
    /// Dual.Common.Core 에서 사용하는 공통 메서드들.  project 참조를 하지 않기 위해서 필요 부분만 발췌
    /// </summary>
    internal static class DualCommonCore
    {
        public static bool IsOneOf(this IComparable key, params IComparable[] set) => set.Any(e => e.CompareTo(key) == 0);
        //internal static bool IsOneOf(this object key, params object[] set) => set.Any(e => e == key);
        public static T Tee<T>(this T input, Action<T> action)
        {
            action(input);
            return input;
        }
    }
}
