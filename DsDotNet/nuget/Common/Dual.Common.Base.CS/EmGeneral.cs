using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;

namespace Dual.Common.Base.CS
{
    public static class EmGeneral
    {
        /// <summary>Indicates whether the specified array is null or has a length of zero.</summary>
        /// <param name="array">The array to test.</param>
        /// <returns>true if the array parameter is null or has a length of zero; otherwise, false.</returns>
        [Pure]
        public static bool IsNullOrEmpty(this Array array)
        {
            return (array == null || array.Length == 0);
        }

        [Pure]
        public static bool IsNullOrEmpty(this string str)
        {
            return (str == null || str == string.Empty);
        }



        [Pure]
        public static bool IsNullOrEmpty(this IEnumerable enumerable)
        {
            return (enumerable == null || enumerable.Cast<object>().Count() == 0);
        }

        /// <summary>
        /// Null 이 아니면서 하나 이상의 element 가졌는지 검사.
        /// </summary>
        [Pure]
        public static bool NonNullAny<TSource>(this IEnumerable<TSource> source)
        {
            if (source == null)
                return false;
            return source.Any();
        }
        [Pure] public static bool NonNullAnyCs<TSource>(this IEnumerable<TSource> source) => NonNullAny(source);

        /// <summary>
        /// Null 이 아니면서 하나 이상의 element 가졌는지 검사.
        /// </summary>
        [Pure]
        public static bool NonNullAny<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null)
                return false;
            return source.Any(predicate);
        }
        [Pure] public static bool NonNullAnyCs<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate) => NonNullAny(source, predicate);

        [Pure]
        public static int Clamp(this int val, int min, int max)
        {
            return val > max ? max : val < min ? min : val;
        }

        public static double Clamp(this double val, double min, double max)
        {
            return val > max ? max : val < min ? min : val;
        }

        [Pure]
        public static bool XOR(this bool val1, bool val2)
        {
            return (val1 && !val2) || (!val1 && val2);
        }

        [Pure]
        public static bool XOR(this object obj1, object obj2)
        {
            return (obj1 != null && obj2 == null) || (obj1 == null && obj2 != null);
        }

        [Pure]
        public static bool Toggle(this bool fact, bool toggle)
        {
            return toggle ? ! fact : fact;
        }

        [Pure]
        public static string NonNullEmptySelector(this string str1, string str2)
        {
            return String.IsNullOrEmpty(str1) ? str2 : str1;
        }


        [Pure]
        public static bool NonNullEqual(this string str1, string str2)
        {
            return !String.IsNullOrEmpty(str1) && !String.IsNullOrEmpty(str2) && str1 == str2;
        }

        [Pure]
        public static bool NullableEqual(this string str1, string str2)
        {
            return (str1.IsNullOrEmpty() && str2.IsNullOrEmpty()) || NonNullEqual(str1, str2);
        }



        public static bool HasTrueValue(this Nullable<bool> nullable)
        {
            return nullable.HasValue && nullable.Value;
        }
    }



}
