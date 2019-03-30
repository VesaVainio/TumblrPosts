using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Model
{
    public static class StringTokenizer
    {
        private static readonly Regex TextSplitRegex = new Regex("[^\\w'?.!]", RegexOptions.Compiled);
        private static readonly Regex TokenSplitRegex = new Regex("([?.!]+)", RegexOptions.Compiled);
        private static readonly Regex ReplaceRegex = new Regex("[?.!]+", RegexOptions.Compiled);

        private static readonly Regex SanitizeRegex = new Regex("[?/\\#]+", RegexOptions.Compiled);

        public static string[] Tokenize(string text)
        {
            string[] words = TextSplitRegex.Split(text.ToLower(CultureInfo.InvariantCulture));
            words = words.SelectMany(x => TokenSplitRegex.Split(x)).Where(x => !string.IsNullOrEmpty(x)).Select(x => ReplaceRegex.Replace(x, ".")).ToArray();

            return words;
        }

        public static string[] GetDigrams(string[] tokens)
        {
            if (tokens == null || tokens.Length < 2)
            {
                return new string[0]; 
            }

            List<string> digrams = new List<string>();

            string previous = tokens[0];

            foreach (string token in tokens.Skip(1))
            {
                if (previous == "." || token == ".")
                {
                    previous = token;
                    continue;
                }

                digrams.Add(previous + " " + token);
                previous = token;
            }

            return digrams.ToArray();
        }

        public static string SanitizeTableKey(string tableKey, string replaceString)
        {
            return SanitizeRegex.Replace(tableKey, replaceString);
        }
    }
}