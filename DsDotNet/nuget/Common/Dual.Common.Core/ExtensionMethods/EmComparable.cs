using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Dual.Common.Core
{
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

        public static int GetCount(this IEnumerable xs)
        {
            if (xs == null)
                return 0;

            int n = 0;
            foreach (var x in xs)
                n++;
            return n;
        }

        public static T Singleton<T>(this IEnumerable<T> xs)
        {
            var xs_ = xs.ToArray();
            if (xs_.Length == 1)
                return xs_[0];
            throw new Exception("Not a singleton");
        }
        public static T Singleton<T>(this IEnumerable<T> xs, Func<T, bool> pred)
        {
            var xs_ = xs.Where(pred).ToArray();
            if (xs_.Length == 1)
                return xs_[0];
            throw new Exception("Not a singleton");
        }
        public static bool IsSingleton<T>(this IEnumerable<T> xs) => xs?.Count() == 1;
        public static bool IsSingleton<T>(this IEnumerable<T> xs, Func<T, bool> pred) => xs?.Where(pred).Count() == 1;

        /// <summary> Key 값이 set 에 포함되는지 여부를 검사한다. </summary>
        public static bool IsOneOf(this IComparable key, params IComparable[] set) => set.Any(e => e.CompareTo(key) == 0);
        public static bool IsOneOf(this object key, params object[] set) => set.Any(e => e == key);
        public static bool IsOneOf(this Type type, params Type[] set) => set.Any(t => t.IsAssignableFrom(type));

    }
}
