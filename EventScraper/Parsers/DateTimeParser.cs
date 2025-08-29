using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace EventScraper.Parsers
{
    public static class DateTimeParser
    {
        private static readonly Dictionary<string, string> SwedishMonths = new()
        {
            { "januari", "01" }, { "februari", "02" }, { "mars", "03" }, { "april", "04" },
            { "maj", "05" }, { "juni", "06" }, { "juli", "07" }, { "augusti", "08" },
            { "september", "09" }, { "oktober", "10" }, { "november", "11" }, { "december", "12" }
        };

        public static (string startTime, string endTime) ParseTimeRange(string timeStr)
        {
            if (string.IsNullOrEmpty(timeStr)) 
                return ("", "");
            
            timeStr = timeStr.Replace("–", "-").Replace("—", "-").Replace("−", "-")
                           .Replace("kl", "").Replace(".", ":");
            
            var parts = timeStr.Split('-');
            var start = parts.Length > 0 ? parts[0].Trim() : "00:00";
            var end = parts.Length > 1 ? parts[1].Trim() : start;
            
            return (start, end);
        }

        public static (DateOnly? startDate, DateOnly? endDate) ParseDateRange(string dateStr)
        {
            if (string.IsNullOrEmpty(dateStr))
                return (null, null);

            dateStr = dateStr.ToLower().Trim();
            
            // Split if there's a range
            var parts = dateStr.Split(new[] { " - ", "-" }, StringSplitOptions.RemoveEmptyEntries);
            
            var startDateStr = parts[0].Trim();
            var endDateStr = parts.Length > 1 ? parts[1].Trim() : startDateStr;

            var startDate = ParseSwedishDate(startDateStr);
            var endDate = ParseSwedishDate(endDateStr);

            return (startDate, endDate);
        }

        public static DateOnly? ParseSwedishDate(string dateStr)
        {
            // Format: "4 juni" eller "4 jun"
            var parts = dateStr.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
                return null;

            var day = parts[0];
            var monthStr = parts[1].ToLower();

            // Matcha mot månader
            var month = SwedishMonths.FirstOrDefault(m => monthStr.StartsWith(m.Key)).Value;
            if (month == null)
                return null;

            if (!int.TryParse(day, out var dayNum))
                return null;

            var year = DateTime.Now.Year;

            if (!DateTime.TryParse($"{year}-{month}-{dayNum}", out var date))
                return null;

            return DateOnly.FromDateTime(date);
        }

        public static (TimeOnly? startTime, TimeOnly? endTime) ParseTimes(string timeStr)
        {
            var (startTimeStr, endTimeStr) = ParseTimeRange(timeStr);

            TimeOnly? startTime = null;
            TimeOnly? endTime = null;

            if (!string.IsNullOrEmpty(startTimeStr))
            {
                if (TimeOnly.TryParse(startTimeStr.Replace(".", ":"), out var parsedStartTime))
                {
                    startTime = parsedStartTime;
                }
            }

            if (!string.IsNullOrEmpty(endTimeStr))
            {
                if (TimeOnly.TryParse(endTimeStr.Replace(".", ":"), out var parsedEndTime))
                {
                    endTime = parsedEndTime;
                }
            }

            return (startTime, endTime);
        }

        public static DateTime? CombineDateAndTime(DateOnly? date, TimeOnly? time)
        {
            if (!date.HasValue || !time.HasValue)
                return null;

            return date.Value.ToDateTime(time.Value);
        }
    }
}
