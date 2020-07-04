using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Console.Utilities
{
    public static class RegexUtil
    {
        public static string Find(string source, string pattern)
        {
            Regex regex = new Regex(pattern);
            var matches = regex.Match(source);
            return matches.Groups[1].Value;
        }

        public static List<string> FindAll(string source, string pattern)
        {
            source = source.Replace("\n", "");
            Regex regex = new Regex(pattern);
            var matches = regex.Matches(source).ToList();
            List<string> found = new List<string>();
            foreach (var match in matches) found.Add(match.Groups[1].Value);
            return found;
        }
    }
}