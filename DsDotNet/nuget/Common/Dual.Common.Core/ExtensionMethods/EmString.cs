using Dual.Common.Base.CS;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Dual.Common.Core
{
    public static class EmString
    {
        public static string[] ParseAgainstRegex(this string s, string regex)
        {
            List<string> tokens = new List<string>();
            var match = Regex.Match(s, regex);
            for (int i = 1; i < match.Groups.Count; i++)
                tokens.Add(match.Groups[i].Value);

            return tokens.ToArray();
        }

        /// <summary>
        /// null / empty 허용 StartsWith version.  null/empty 인 경우 false 반환
        /// </summary>
        public static bool StartsWithEx(this string s, string prefix)
        {
            if (s.IsNullOrEmpty() || prefix.IsNullOrEmpty())
                return false;

            return s.StartsWith(prefix);
        }

        /// <summary>
        /// null / empty 허용 EndsWith version.  null/empty 인 경우 false 반환
        /// </summary>
        public static bool EndsWithEx(this string s, string suffix)
        {
            if (s.IsNullOrEmpty() || suffix.IsNullOrEmpty())
                return false;

            return s.EndsWith(suffix);
        }


        /// <summary>
        /// begin 과 end 로 둘러싸인 s 에서 begin 과 end 를 제거한 가운데 값만을 뽑아내어 반환
        /// </summary>
        public static string Strip(this string s, string begin, string end)
	    {
			if (s.StartsWithEx(begin) && s.EndsWithEx(end))
			{
				var length = s.Length - begin.Length - end.Length;
				return new string(s.Skip(begin.Length).Take(length).ToArray());
			}

		    return null;
	    }

        public static Nullable<int> TryParseInt(this string s)
        {
            int result = 0;
            if (int.TryParse(s, out result))
                return result;
            return null;
        }


        // https://stackoverflow.com/questions/7148768/string-split-by-index-params
        public static IEnumerable<string> SplitAt(this string source, params int[] index)
        {
            var indices = new[] { 0 }.Union(index).Union(new[] { source.Length });

            return indices
                        .Zip(indices.Skip(1), (a, b) => Tuple.Create(a, b))
                        .Select(tpl => source.Substring(tpl.Item1, tpl.Item2 - tpl.Item1));

            /*
            var s = "abcd";

            s.SplitAt(); // "abcd"
            s.SplitAt(0); // "abcd"
            s.SplitAt(1); // "a", "bcd"
            s.SplitAt(2); // "ab", "cd"
            s.SplitAt(1, 2) // "a", "b", "cd"
            s.SplitAt(3); // "abc", "d"
             */
        }


        /// <summary>
        /// string.Join 을 pipe line 처리하기 위한 extension method
        /// </summary>
        public static string JoinString(this IEnumerable<string> sources, string separator) => string.Join(separator, sources);

        /// <summary>
        /// 주어진 문자열을 line 단위로 split
        /// </summary>
        public static IEnumerable<string> SplitByLines(this string text, StringSplitOptions splitOption=StringSplitOptions.RemoveEmptyEntries)
            => text.Split(new[] { '\r', '\n' }, splitOption);

        public static string SkipNChar(this string text, int n)
        {
            var len = text.Length;
            if (n < 0 || n >= len )
                return null;
            return text.Substring(n, len - n);
        }

        public static bool EqualsIgnoringCase(this string x, string y) => string.Equals(x, y, StringComparison.OrdinalIgnoreCase);
        public static bool EqualsCaseInsensitive(this string x, string y) => EqualsIgnoringCase(x, y);

    }
}
