using System;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Dsu.Common.CS.LSIS.ExtensionMethods
{
    /// <summary>
    /// 범용 Comparable 관련 extension methods
    /// </summary>
    /// @ingroup groupCommon
    public static class EmComparable
    {
        /// <summary>
        /// [min, max]
        /// value 값이 from, to 사이에 존재하는지를 검사한다.
        /// http://stackoverflow.com/questions/8776624/if-value-in-rangex-y-function-c-sharp
        /// </summary>
        [Pure]
        public static bool InClosedRange<T>(this T val, T min, T max) where T : IComparable<T>
        {
            Contract.Requires(min.CompareTo(max) <= 0);
            return min.CompareTo(val) <= 0 && val.CompareTo(max) <= 0;
        }

        [Pure]
        public static bool InRange<T>(this T val, T min, T max) where T : IComparable<T>
        {
            return val.InClosedRange(min, max);
        }

        /// <summary> [min, max) </summary>
        [Pure]
        public static bool InClampRange<T>(this T val, T min, T max) where T : IComparable<T>
        {
            Contract.Requires(min.CompareTo(max) <= 0);
            return min.CompareTo(val) <= 0 && val.CompareTo(max) < 0;
        }

        /// <summary> (min, max) </summary>
        [Pure]
        public static bool InOpenRange<T>(this T val, T min, T max) where T : IComparable<T>
        {
            Contract.Requires(min.CompareTo(max) <= 0);
            return min.CompareTo(val) < 0 && val.CompareTo(max) < 0;
        }


        [Pure]
        public static bool EpsilonEqual(this double value1, double value2, double epsilon = Double.Epsilon)
        {
            return Math.Abs(value1 - value2) < epsilon;
        }

        [Pure]
        public static bool EpsilonEqual(this float value1, float value2, float epsilon = Single.Epsilon)
        {
            return Math.Abs(value1 - value2) < epsilon;
        }

        /// <summary> Key 값이 set 에 포함되는지 여부를 검사한다. </summary>
        [Pure]
        public static bool IsOneOf(this IComparable key, params IComparable[] set)
        {
            return set.Any(e => e.CompareTo(key) == 0);
        }


        public static bool IsOneOf(this object key, params object[] set)
        {
            return set.Any(e => e == key);
        }

        public static bool IsOneOf(this Type type, params Type[] set)
        {
            return set.Any(t => t.IsAssignableFrom(type));
        }

    }
}
