using System;
using System.Collections;
using System.Collections.Generic;

namespace Dsu.Common.Utilities.Core.ExtensionMethods
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
                Console.WriteLine(IsFlagDefined(side) + " " + side);
            }

            bool defined = Enum.IsDefined(typeof(BorderSide), (BorderSide) 12345);      // should be false
        }

	    public static IEnumerable<T> GetValues<T>() => Enum.GetValues(typeof(T)).ToEnumerable<T>();

    }
}
