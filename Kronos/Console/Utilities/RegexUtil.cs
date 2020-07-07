using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Console.Utilities
{
    public static class RegexUtil
    {
        public static string Find(string source, string pattern)
        {
            var regex = new Regex(pattern);
            var matches = regex.Match(source);
            return matches.Groups[1].Value;
        }

        public static List<string> FindAll(string source, string pattern)
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