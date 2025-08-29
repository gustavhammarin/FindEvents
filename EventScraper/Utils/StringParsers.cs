using System;

namespace EventScraper.Utils;

public static class StringParsers
{
    public static string CleanSeparators(this string input)
    {
        return System.Text.RegularExpressions.Regex.Replace(input, @"[–\-\|]", "").Trim();
    }


}
