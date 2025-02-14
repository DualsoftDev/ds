using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Dual.Common.Core
{
    public static class StringHelper
    {
        /// <summary>
        /// A case insensitive replace function.  http://stackoverflow.com/questions/244531/is-there-an-alternative-to-string-replace-that-is-case-insensitive
        /// </summary>
        /// <param name="originalString">The string to examine.(HayStack)</param>
        /// <param name="oldValue">The value to replace.(Needle)</param>
        /// <param name="newValue">The new value to be inserted</param>
        /// <returns>A string</returns>
        public static string ReplaceIgnoreCase(string originalString, string oldValue, string newValue)
        {
            Regex regEx = new Regex(oldValue,
               RegexOptions.IgnoreCase | RegexOptions.Multiline);
            return regEx.Replace(originalString, newValue);
        }
        public static IEnumerable<string> SplitString(this string text, char separator)
            => text.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries);
        public static IEnumerable<string> SplitString(this string text, string separator)
            => text.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries);
    }


    /*
     * Custom string'able comparer
     *  - T should support ToString() method for key : see StringComparerTester below
     */
    public class StringEqualityComparer<T> : IEqualityComparer<T>
    {
        // Products are equal if their names and product numbers are equal.
        public bool Equals(T x, T y)
        {

            //Check whether the compared objects reference the same data.
            if (Object.ReferenceEquals(x, y)) return true;

            //Check whether any of the compared objects is null.
            if (Object.ReferenceEquals(x, null) || Object.ReferenceEquals(y, null))
                return false;

            //Check whether the products' properties are equal.
            return x.ToString() == y.ToString();
        }

        // If Equals() returns true for a pair of objects
        // then GetHashCode() must return the same value for these objects.

        public int GetHashCode(T product)
        {
            //Check whether the object is null
            if (Object.ReferenceEquals(product, null)) return 0;

            //Get hash code for the Name field if it is not null.
            return product.ToString().GetHashCode();
        }

    }

    public class StringComparer<T> : IComparer<T>
    {
        public int Compare(T x, T y)
        {
            // Should I test if 'One == null'  or  'Two == null'  ????
            return String.Compare(x.ToString(), y.ToString(), false); // Caseinsesitive = false
        }
    }
}
