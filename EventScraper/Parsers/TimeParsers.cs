using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace EventScraper.Parsers;

public class TimeParsers
{
    private static readonly string[] formatsArray = new[] { "H.mm", "HH.mm", "H:mm", "HH:mm", "H", "HH" };

    public static (TimeOnly? Start, TimeOnly? End) TimeParser(string input)
    {
        Console.WriteLine($"Raw time string: '{input}'");

        if (string.IsNullOrWhiteSpace(input))
            return (null, null);

        // 🧼 Rensa bort oönskade ord
        var raw = input
            .Replace("klockan", "", StringComparison.OrdinalIgnoreCase)
            .Replace("Klockan", "", StringComparison.OrdinalIgnoreCase)
            .Replace("Kl.", "", StringComparison.OrdinalIgnoreCase)
            .Replace("Kl", "", StringComparison.OrdinalIgnoreCase)
            .Replace("kl.", "", StringComparison.OrdinalIgnoreCase)
            .Replace("kl", "", StringComparison.OrdinalIgnoreCase)
            .Replace('\u00A0', ' ') // non-breaking space
            .Trim();

        var timeMatches = Regex.Matches(raw, @"\b\d{1,2}([:.\u00A0]?\d{0,2})?\b");

            if (timeMatches.Count > 0)
            {
                raw = string.Join("-", timeMatches
                    .Select(m => m.Value.Replace('.', ':')));
            }
            else
            {
                return (null, null);
            }

        // 🪓 Dela upp på bindestreck
        char[] separators = new[] { '-', '\u2013', '\u2014' }; // -, –, —
        var parts = raw
            .Split(separators, StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .ToArray();

        if (parts.Length == 0)
            return (null, null);

        // 🕒 Lägg till ":00" om bara timme anges
        for (int i = 0; i < parts.Length; i++)
        {
            if (Regex.IsMatch(parts[i], @"^\d{1,2}$"))
            {
                parts[i] += ":00";
            }
        }

        // 🧠 Försök parsa tiderna
        TimeOnly? start = TryParseTime(parts[0]);
        TimeOnly? end = parts.Length > 1 ? TryParseTime(parts[1]) : null;

        // Om start och end är samma → bara en tid
        if (start.HasValue && end.HasValue && start == end)
            end = null;

        return (start, end);
    }

    private static TimeOnly? TryParseTime(string input)
    {
        foreach (var format in formatsArray)
        {
            if (TimeOnly.TryParseExact(input, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
                return result;
        }
        return null;
    }
}
