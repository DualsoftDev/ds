using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Dual.Common.Base.CS
{
    public static class EmEnum
    {
        /// <summary>
        /// Flag type enumeration 값이 define 되어 있는지를 검사한다.
        /// C# 6.0 in a Nutshell.pdf, pp. 113
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static bool IsFlagDefined(Enum e)
        {
            decimal d;
            return !decimal.TryParse(e.ToString(), out d);
        }

        [Flags]
        private enum BorderSides { Left = 1, Right = 2, Top = 4, Bottom = 8 }
        private enum BorderSide { Left, Right, Top, Bottom }
        private static void ShowIsFlagDefinedUsage()
        {
            for (int i = 0; i <= 16; i++)
            {
                BorderSides side = (BorderSides)i;
                System.Console.WriteLine(IsFlagDefined(side) + " " + side);
            }

            bool defined = Enum.IsDefined(typeof(BorderSide), (BorderSide) 12345);      // should be false
        }

	    public static IEnumerable<T> GetValues<T>() => Enum.GetValues(typeof(T)).ToEnumerable<T>();

        public static bool Has<T>(this System.Enum type, T value)
        {
            try
            {
                return (((int)(object)type & (int)(object)value) == (int)(object)value);
            }
            catch
            {
                return false;
            }
        }

        public static bool Is<T>(this System.Enum type, T value)
        {
            try
            {
                return (int)(object)type == (int)(object)value;
            }
            catch
            {
                return false;
            }
        }


        public static T Add<T>(this System.Enum type, T value)
        {
            try
            {
                return (T)(object)(((int)(object)type | (int)(object)value));
            }
            catch (Exception ex)
            {
                throw new ArgumentException(
                    string.Format(
                        "Could not append value from enumerated type '{0}'.",
                        typeof(T).Name
                        ), ex);
            }
        }


        public static T Remove<T>(this System.Enum type, T value)
        {
            try
            {
                return (T)(object)(((int)(object)type & ~(int)(object)value));
            }
            catch (Exception ex)
            {
                throw new ArgumentException(
                    string.Format(
                        "Could not remove value from enumerated type '{0}'.",
                        typeof(T).Name
                        ), ex);
            }
        }

        public static IEnumerable<T> Do<T>(this IEnumerable<T> xs, Action<T> action)
        {
            var xxs = xs.ToArray();
            foreach (var x in xxs)
                action(x);
            return xxs;
        }
        public static IEnumerable<T> Tap<T>(this IEnumerable<T> xs, Action<T> action) => Do(xs, action);

        /// <summary>
        /// Seq 에서 중복된 항목들만 추려서 반환
        /// </summary>
        public static IEnumerable<T> FindDuplicates<T>(this IEnumerable<T> xs)
        {
            return
                xs.GroupBy(x => x)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    ;
        }
        /// <summary>
        /// Seq 에 중복된 항목 존재 여부 반환
        /// </summary>
        public static bool ContainsDuplicates<T>(this IEnumerable<T> xs) => xs.FindDuplicates().Any();

    }
}
