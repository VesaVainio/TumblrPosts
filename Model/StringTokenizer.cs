using System;

namespace Model
{
    public static class StringTokenizer
    {
        private static readonly char[] DelimiterChars = {' ', ',', '.', ':', '\n', '-', ';'};

        public static string[] Tokenize(string text)
        {
            string[] words = text.Split(DelimiterChars, StringSplitOptions.RemoveEmptyEntries);
            return words;
        }
    }
}