using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Dual.Common.Core
{
    public static class Parser
    {
        private static string _blockComments = @"/\*(.*?)\*/";
        private static string _lineComments = @"//(.*?)\r?\n";

        static string RemoveComments(this string input, IEnumerable<string> commentPatterns, IEnumerable<string> startStrings)
        {
            var patterns = String.Join("|", commentPatterns);
            string noComments = Regex.Replace(input, patterns,
                me =>
                {
                    if (startStrings.Any(s => me.Value.StartsWith(s)))
                        return "";
                    // Keep the literal strings
                    return me.Value;
                },
                RegexOptions.Singleline);

            return noComments;
        }

        public static string RemoveCSharpComments(this string input) => RemoveComments(input, new[] { _blockComments, _lineComments }, new[] { "/*", "//" });
        public static string RemoveSqlComments(this string input) => RemoveComments(input, new[] { _blockComments, @"\-\-(.*?)\r?\n", @"#(.*?)\r?\n" }, new[] {"/*", "--", "#"});
    }
}
