using System.Text.RegularExpressions;
using MySociety.Application.Common.Exceptions;

namespace MySociety.Application.Contributions;

public static partial class ContributionMonthRange
{
    public const int MaxMonths = 24;

    public static bool TryValidate(
        string fromMonth,
        string toMonth,
        out string periodKey,
        out int monthCount,
        out string error)
    {
        periodKey = string.Empty;
        monthCount = 0;

        if (!TryParseMonth(fromMonth, out var fromYear, out var fromMon))
        {
            error = "From month must be yyyy-MM (e.g. 2026-05).";
            return false;
        }

        if (!TryParseMonth(toMonth, out var toYear, out var toMon))
        {
            error = "To month must be yyyy-MM (e.g. 2026-07).";
            return false;
        }

        var fromIndex = fromYear * 12 + fromMon;
        var toIndex = toYear * 12 + toMon;
        if (fromIndex > toIndex)
        {
            error = "From month cannot be after to month.";
            return false;
        }

        monthCount = toIndex - fromIndex + 1;
        if (monthCount > MaxMonths)
        {
            error = $"A contribution request can cover at most {MaxMonths} months.";
            return false;
        }

        periodKey = BuildPeriodKey(fromMonth.Trim(), toMonth.Trim());
        error = string.Empty;
        return true;
    }

    public static string BuildPeriodKey(string fromMonth, string toMonth) =>
        $"{fromMonth.Trim()}..{toMonth.Trim()}";

    public static int CountInclusiveMonths(string fromMonth, string toMonth)
    {
        if (!TryParseMonth(fromMonth, out var fromYear, out var fromMon) ||
            !TryParseMonth(toMonth, out var toYear, out var toMon))
        {
            throw new ValidationException("Invalid month range.");
        }

        return toYear * 12 + toMon - (fromYear * 12 + fromMon) + 1;
    }

    public static string AddMonths(string yyyyMm, int monthsToAdd)
    {
        if (!TryParseMonth(yyyyMm, out var year, out var month))
        {
            throw new ValidationException("Invalid month.");
        }

        var index = year * 12 + (month - 1) + monthsToAdd;
        var newYear = index / 12;
        var newMonth = index % 12 + 1;
        return $"{newYear:D4}-{newMonth:D2}";
    }

    public static IReadOnlyList<string> GetRecentMonths(int count = 12, DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;
        var months = new List<string>(count);
        var year = now.Year;
        var month = now.Month;

        for (var i = 0; i < count; i++)
        {
            months.Add($"{year:D4}-{month:D2}");
            if (month == 1)
            {
                year--;
                month = 12;
            }
            else
            {
                month--;
            }
        }

        months.Reverse();
        return months;
    }

    public static string FormatDisplayLabel(string period)
    {
        if (period.Contains("..", StringComparison.Ordinal))
        {
            var parts = period.Split("..", 2, StringSplitOptions.TrimEntries);
            if (parts.Length == 2 &&
                TryParseMonth(parts[0], out var fy, out var fm) &&
                TryParseMonth(parts[1], out var ty, out var tm))
            {
                var count = CountInclusiveMonths(parts[0], parts[1]);
                return $"{FormatMonth(fy, fm)} – {FormatMonth(ty, tm)} ({count} mo)";
            }
        }

        if (TryParseMonth(period, out var y, out var m))
        {
            return FormatMonth(y, m);
        }

        return period;
    }

    private static string FormatMonth(int year, int month) =>
        new DateTime(year, month, 1).ToString("MMM yyyy");

    public static bool TryParseMonth(string value, out int year, out int month)
    {
        year = 0;
        month = 0;
        value = value.Trim();
        var match = MonthRegex().Match(value);
        if (!match.Success)
        {
            return false;
        }

        year = int.Parse(match.Groups[1].Value);
        month = int.Parse(match.Groups[2].Value);
        return true;
    }

    public static bool TryParsePeriodKey(string periodKey, out int fromIndex, out int toIndex)
    {
        fromIndex = 0;
        toIndex = 0;

        var trimmed = periodKey.Trim();
        string fromMonth;
        string toMonth;

        if (trimmed.Contains("..", StringComparison.Ordinal))
        {
            var parts = trimmed.Split("..", 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2)
            {
                return false;
            }

            fromMonth = parts[0];
            toMonth = parts[1];
        }
        else
        {
            fromMonth = trimmed;
            toMonth = trimmed;
        }

        if (!TryParseMonth(fromMonth, out var fromYear, out var fromMon) ||
            !TryParseMonth(toMonth, out var toYear, out var toMon))
        {
            return false;
        }

        fromIndex = ToMonthIndex(fromYear, fromMon);
        toIndex = ToMonthIndex(toYear, toMon);
        return fromIndex <= toIndex;
    }

    public static bool MonthRangesOverlap(string fromMonth, string toMonth, string existingPeriodKey)
    {
        if (!TryParseMonth(fromMonth, out var fromYear, out var fromMon) ||
            !TryParseMonth(toMonth, out var toYear, out var toMon) ||
            !TryParsePeriodKey(existingPeriodKey, out var existingFromIndex, out var existingToIndex))
        {
            return false;
        }

        var requestFromIndex = ToMonthIndex(fromYear, fromMon);
        var requestToIndex = ToMonthIndex(toYear, toMon);
        return requestFromIndex <= existingToIndex && existingFromIndex <= requestToIndex;
    }

    public static bool TryFindOverlappingPeriod(
        string fromMonth,
        string toMonth,
        IEnumerable<string> existingPeriodKeys,
        out string overlappingPeriod)
    {
        foreach (var existing in existingPeriodKeys)
        {
            if (MonthRangesOverlap(fromMonth, toMonth, existing))
            {
                overlappingPeriod = existing;
                return true;
            }
        }

        overlappingPeriod = string.Empty;
        return false;
    }

    private static int ToMonthIndex(int year, int month) => year * 12 + month;

    [GeneratedRegex(@"^(\d{4})-(0[1-9]|1[0-2])$")]
    private static partial Regex MonthRegex();
}
