using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace Dsu.Common.CS.LSIS.ExtensionMethods
{
    /// <summary>
    /// 범용 extension methods
    /// </summary>
    /// @ingroup groupCommon
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
        public static bool IsNullOrEmpty(this IEnumerable enumerable)
        {
            return (enumerable == null || ! enumerable.Cast<object>().Any());
        }

        [Pure]
        public static bool NonNullAny<TSource>(this IEnumerable<TSource> source)
        {
            if (source == null)
                return false;
            return source.Any();
        }

        [Pure]
        public static bool NonNullAny<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null)
                return false;
            return source.Any(predicate);
        }

        [Pure]
        public static int Clamp(this int val, int min, int max)
        {
            if (val > max)
                return max;
            if (val < min)
                return min;
            return val;
        }

        public static double Clamp(this double val, double min, double max)
        {
            if (val > max)
                return max;
            if (val < min)
                return min;
            return val;
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









    /// <summary>
    /// http://stackoverflow.com/questions/12447156/how-can-i-set-the-column-width-of-a-property-grid
    /// </summary>
    public static class PropertyGridColumnWidthSetter
    {
        public static void SetLabelColumnWidth(this PropertyGrid grid, int width)
        {
            if (grid == null)
                return;

            FieldInfo fi = grid.GetType().GetField("gridView", BindingFlags.Instance | BindingFlags.NonPublic);
            if (fi == null)
                return;

            Control view = fi.GetValue(grid) as Control;
            if (view == null)
                return;

            MethodInfo mi = view.GetType().GetMethod("MoveSplitterTo", BindingFlags.Instance | BindingFlags.NonPublic);
            if (mi == null)
                return;
            mi.Invoke(view, new object[] {width});
        }
    }
}
