using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Model
{
    public static class StringTokenizer
    {
        private static readonly Regex SplitRegex = new Regex("[^\\w']", RegexOptions.Compiled);

        public static string[] Tokenize(string text)
        {
            string[] words = SplitRegex.Split(text.ToLower(CultureInfo.InvariantCulture));
            words = words.Where(x => !string.IsNullOrEmpty(x)).ToArray();

            return words;
        }
    }
}