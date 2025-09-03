using System;
using System.Collections.Generic;
using System.Globalization;

namespace EventScraper.Utils
{
    public static class DateParser
    {
        // Mappning svenska månadnamn → månadstal
        private static readonly Dictionary<string, int> MonthMap = new(StringComparer.OrdinalIgnoreCase)
        {
            ["januari"] = 1,
            ["februari"] = 2,
            ["mars"] = 3,
            ["april"] = 4,
            ["maj"] = 5,
            ["juni"] = 6,
            ["juli"] = 7,
            ["augusti"] = 8,
            ["september"] = 9,
            ["oktober"] = 10,
            ["november"] = 11,
            ["december"] = 12,
            ["jan"] = 1,
            ["feb"] = 2,
            ["mar"] = 3,
            ["apr"] = 4,
            ["maj"] = 5,
            ["jun"] = 6,
            ["jul"] = 7,
            ["aug"] = 8,
            ["sep"] = 9,
            ["okt"] = 10,
            ["nov"] = 11,
            ["dec"] = 12,
        };

        /// <summary>
        /// Parserar en svensk datumsträng som antingen är ett enkeldatum
        /// eller ett intervall (t.ex. "10–12 augusti 2025" eller "10 augusti–3 september").
        /// Årtal tas från sista delen eller sätts till innevarande år.
        /// </summary>
        public static (DateOnly? Start, DateOnly? End) ParseSwedishDateRange(string input)
        {
            // 1) Dela på bindestreck (– eller -)
            var separators = new[] { '–', '-' };
            var parts = input.Split(separators, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1)
            {
                // Endast ett datum
                var single = ParseSingle(parts[0].Trim());
                // Sista dag i samma månad om man vill (eller sätt End = Start)
                return (single, single);
            }

            // 2) Intervall med två delar: left och right
            var leftText = parts[0].Trim();
            var rightText = parts[1].Trim();

            // 3) Parsning av högerdel (fullständigt datum eller med månad/år)
            var right = ParseSingle(rightText);

            // 4) Parsning av vänsterdel, fyll på med månad/år från höger om saknas
            var left = ParseSingleWithDefaults(leftText, right);

            return (left, right);
        }

        // Parserar "DD MMMM YYYY?" – kastar om formatet är helt fel
        private static DateOnly? ParseSingle(string text)
        {
            if (text.Contains('$'))
            {
                return null;
            }

            Console.WriteLine(text);
            var tokens = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length < 2)
                throw new FormatException($"Förväntar minst dag + månad i '{text}'"); 

            // Dag
            if (!int.TryParse(tokens[0], out var day))
                throw new FormatException($"Ogiltig dag: '{tokens[0]}'");

            // Månad
            if (!MonthMap.TryGetValue(tokens[1], out var month))
                throw new FormatException($"Ogiltigt månadsnamn: '{tokens[1]}'");

            // År – om inte angivet, använd innevarande år
            var year = DateTime.Now.Year;
            if (tokens.Length >= 3 && int.TryParse(tokens[2], out var y))
                year = y;

            return new DateOnly(year, month, day);
        }

        // Parserar vänsterdel och använder månad/år från right om texten bara är "DD"
        private static DateOnly? ParseSingleWithDefaults(string text, DateOnly? right)
        {
            var tokens = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            // Om bara dag, använd month/year från right
            if (tokens.Length == 1 && int.TryParse(tokens[0], out var day))
                return new DateOnly(right?.Year ?? 0, right?.Month ?? 0, day);

            // Annars vanlig parsing och fyll på år om det saknas
            var date = ParseSingle(text);
            if (date?.Year == DateTime.Now.Year && text.Split(' ').Length < 3)
            {
                // Om ParseSingle antog innevarande år men texten inte innehöll år,
                // och right-delen har ett annat år, låna det
                date = new DateOnly(right?.Year ?? 0, date?.Month ?? 0, date?.Day ?? 0);
            }
            return date;
        }

        public static (DateOnly? dateOnly, TimeOnly? timeOnly) ParseDateTimeToDateOnlyTimeOnly(DateTime dateTime)
        {
            DateOnly dateOnly = DateOnly.FromDateTime(dateTime);
            TimeOnly timeOnly = TimeOnly.FromDateTime(dateTime);

            return (dateOnly, timeOnly);    
        }
    }
}