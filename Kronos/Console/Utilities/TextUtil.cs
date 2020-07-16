using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Console.Utilities
{
    /// <summary> Common utilities for manipulating strings </summary>
    public static class TextUtil
    {
        /// <summary> Capitalize every first letter of every word in a sentence </summary>
        public static string ToTitleCase(string s)
        {
            var textInfo = new CultureInfo("en-UK", false).TextInfo;
            return textInfo.ToTitleCase(s);
        }

        /// <summary>
        ///     Apply a regex pattern to a source string, and retrieve the first group from the first match
        ///     Used to extract XML data
        /// </summary>
        public static string Find(this string source, string pattern)
        {
            var regex = new Regex(pattern);
            var matches = regex.Match(source);
            return matches.Groups[1].Value;
        }

        /// <summary>
        ///     Apply a regex pattern to a source string, and retrieve a list containing the first group for every match
        ///     Used to extract XML data
        /// </summary>
        public static List<string> FindAll(this string source, string pattern)
        {
            source = source.Replace("\n", "");
            var regex = new Regex(pattern);
            var matches = regex.Matches(source).ToList();
            var found = new List<string>();
            foreach (var match in matches) found.Add(match.Groups[1].Value);
            return found;
        }
    }
}