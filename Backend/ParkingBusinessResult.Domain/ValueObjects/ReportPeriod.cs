using System.Globalization;

namespace ParkingBusinessResult.Domain.ValueObjects;

public sealed record ReportPeriod(DateOnly ReportDate, string PeriodType)
{
    public static ReportPeriod? Parse(string value)
    {
        if (DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly day))
            return new(day, "DAILY");
        if (DateTime.TryParseExact(value, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime month))
            return new(DateOnly.FromDateTime(month), "MONTHLY");
        if (value.Length == 7 && value[0] == 'Q' && value[1] is >= '1' and <= '4' && value[2] == '-' &&
            int.TryParse(value.AsSpan(3), NumberStyles.None, CultureInfo.InvariantCulture, out int quarterYear) && quarterYear is >= 1 and <= 9999)
            return new(new DateOnly(quarterYear, (value[1] - '1') * 3 + 1, 1), "QUARTERLY");
        if (value.Length == 4 && int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int year) && year is >= 1 and <= 9999)
            return new(new DateOnly(year, 1, 1), "YEARLY");
        return null;
    }

    public string? Validate(DateOnly today)
    {
        if (ReportDate > today) return "Ngày báo cáo không được lớn hơn ngày hiện tại.";
        if (PeriodType is not ("DAILY" or "MONTHLY" or "QUARTERLY" or "YEARLY"))
            return "Loại kỳ báo cáo không hợp lệ.";
        return null;
    }

    public string PeriodValue => PeriodType switch
    {
        "DAILY" => ReportDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        "MONTHLY" => ReportDate.ToString("yyyy-MM", CultureInfo.InvariantCulture),
        "QUARTERLY" => $"Q{(ReportDate.Month - 1) / 3 + 1}-{ReportDate.Year:D4}",
        "YEARLY" => ReportDate.Year.ToString("D4", CultureInfo.InvariantCulture),
        _ => throw new InvalidOperationException("Unsupported reporting period type.")
    };

    public string? PreviousPeriodValue()
    {
        try
        {
            DateOnly previous = PeriodType switch
            {
                "DAILY" => ReportDate.AddDays(-1),
                "MONTHLY" => ReportDate.AddMonths(-1),
                "QUARTERLY" => ReportDate.AddMonths(-3),
                "YEARLY" => ReportDate.AddYears(-1),
                _ => throw new InvalidOperationException("Unsupported reporting period type.")
            };
            return new ReportPeriod(previous, PeriodType).PeriodValue;
        }
        catch (ArgumentOutOfRangeException) { return null; }
    }

    public int DaysInPeriod => PeriodType switch
    {
        "DAILY" => 1,
        "MONTHLY" => DateTime.DaysInMonth(ReportDate.Year, ReportDate.Month),
        "QUARTERLY" => Enumerable.Range((ReportDate.Month - 1) / 3 * 3 + 1, 3).Sum(month => DateTime.DaysInMonth(ReportDate.Year, month)),
        "YEARLY" => DateTime.IsLeapYear(ReportDate.Year) ? 366 : 365,
        _ => throw new InvalidOperationException("Unsupported reporting period type.")
    };
}
