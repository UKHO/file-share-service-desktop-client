using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using UKHO.FileShareService.DesktopClient.Core;
using UKHO.WeekNumberUtils;

namespace UKHO.FileShareService.DesktopClient.Helper
{
    public class MacroTransformer : IMacroTransformer
    {
        private readonly ICurrentDateTimeProvider currentDateTimeProvider;

        public MacroTransformer(ICurrentDateTimeProvider currentDateTimeProvider)
        {
            this.currentDateTimeProvider = currentDateTimeProvider;
        }

        public string ExpandMacros(string value)
        {
            Func<Match, string> now_Year = (match) =>
            {
                return currentDateTimeProvider.CurrentDateTime.Year.ToString();
            };
            Func<Match, string> nowAddDays_Year = (match) =>
            {
                var capturedNumber = match.Groups[1].Value;
                var dayOffset = int.Parse(capturedNumber.Replace(" ", ""));
                return currentDateTimeProvider.CurrentDateTime.AddDays(dayOffset).Year.ToString();
            };
            Func<Match, string> now_WeekNumber = (match) =>
            {
                return WeekNumber.GetUKHOWeekFromDateTime(currentDateTimeProvider.CurrentDateTime).Week.ToString();
            };
            Func<Match, string> now_WeekNumberYear = (match) =>
            {
                return WeekNumber.GetUKHOWeekFromDateTime(currentDateTimeProvider.CurrentDateTime).Year.ToString();
            };
            Func<Match, string> now_WeekNumberPlusWeeks = (match) =>
            {
                var capturedNumber = match.Groups[1].Value;
                var dayOffset = int.Parse(capturedNumber.Replace(" ", ""));
                return WeekNumber.GetUKHOWeekFromDateTime(currentDateTimeProvider.CurrentDateTime.AddDays(dayOffset * 7)).Week.ToString();
            };
            Func<Match, string> now_WeekNumberPlusWeeksYear = (match) =>
            {
                var capturedNumber = match.Groups[1].Value;
                var dayOffset = int.Parse(capturedNumber.Replace(" ", ""));
                return WeekNumber.GetUKHOWeekFromDateTime(currentDateTimeProvider.CurrentDateTime.AddDays(dayOffset * 7)).Year.ToString();
            };
            Func<Match, string> nowAddDays_Week = (match) =>
            {
                var capturedNumber = match.Groups[1].Value;
                var dayOffset = int.Parse(capturedNumber.Replace(" ", ""));
                return WeekNumber.GetUKHOWeekFromDateTime(currentDateTimeProvider.CurrentDateTime.AddDays(dayOffset)).Week.ToString();
            };
            Func<Match, string> nowAddDays_WeekYear = (match) =>
            {
                var capturedNumber = match.Groups[1].Value;
                var dayOffset = int.Parse(capturedNumber.Replace(" ", ""));
                return WeekNumber.GetUKHOWeekFromDateTime(currentDateTimeProvider.CurrentDateTime.AddDays(dayOffset)).Year.ToString();
            };
            Func<Match, string> nowAddDays_Date = (match) =>
            {
                var capturedNumber = match.Groups[1].Value;
                var dayOffset = int.Parse(capturedNumber.Replace(" ", ""));
                return currentDateTimeProvider.CurrentDateTime.AddDays(dayOffset)
                    .ToString(CultureInfo.InvariantCulture);
            };
            Func<Match, string> now_Date = (match) =>
            {
                return currentDateTimeProvider.CurrentDateTime.ToString(CultureInfo.InvariantCulture);
            };


            var replacementExpressions = new Dictionary<string, Func<Match, string>>
            {
                {@"\$\(\s*now\.Year\s*\)", now_Year },
                {@"\$\(\s*now\.Year2\s*\)", (match) => now_Year(match).Substring(2,2) },
                {@"\$\(\s*now.AddDays\(\s*([+-]?\s*\d+)\s*\).Year\s*\)", nowAddDays_Year},
                {@"\$\(\s*now.AddDays\(\s*([+-]?\s*\d+)\s*\).Year2\s*\)", (match) => nowAddDays_Year(match).Substring(2,2)},
                {@"\$\(\s*now\.WeekNumber\s*\)",now_WeekNumber },
                {@"\$\(\s*now\.WeekNumber\.Year\s*\)", now_WeekNumberYear },
                {@"\$\(\s*now\.WeekNumber\.Year2\s*\)", (match) => now_WeekNumberYear(match).Substring(2,2) },
                {@"\$\(\s*now\.WeekNumber\s*([+-]\s*\d+)\)", now_WeekNumberPlusWeeks },
                {@"\$\(\s*now\.WeekNumber\s*([+-]\s*\d+)\.Year\)", now_WeekNumberPlusWeeksYear},
                {@"\$\(\s*now\.WeekNumber\s*([+-]\s*\d+)\.Year2\)", (match) => now_WeekNumberPlusWeeksYear(match).Substring(2,2) },
                {@"\$\(\s*now.AddDays\(\s*([+-]?\s*\d+)\s*\).WeekNumber\s*\)",nowAddDays_Week },
                {@"\$\(\s*now.AddDays\(\s*([+-]?\s*\d+)\s*\).WeekNumber\.Year\s*\)", nowAddDays_WeekYear },
                {@"\$\(\s*now.AddDays\(\s*([+-]?\s*\d+)\s*\).WeekNumber\.Year2\s*\)", (match) => nowAddDays_WeekYear(match).Substring(2,2)},
                {@"\$\(now.AddDays\(\s*([+-]?\s*\d+)\s*\)\)", nowAddDays_Date },
                {@"\$\(now\)", now_Date}
            };

            if (string.IsNullOrEmpty(value))
                return value;

            return replacementExpressions.Aggregate(value,
                (input, kv) =>
                {
                    var match = Regex.Match(input, kv.Key);
                    while (match.Success)
                    {
                        var end = Math.Min(match.Index + match.Length, input.Length);
                        input = input[..match.Index] +
                                match.Result(kv.Value(match)) +
                                input[end..];

                        match = Regex.Match(input, kv.Key);
                    }

                    return input;
                });
        }
    }
}
