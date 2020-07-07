using System.Globalization;

namespace Console.Utilities
{
    public class TextUtil
    {
        public static string ToTitleCase(string s)
        {
            var textInfo = new CultureInfo("en-UK", false).TextInfo;
            return textInfo.ToTitleCase(s);
        }
    }
}