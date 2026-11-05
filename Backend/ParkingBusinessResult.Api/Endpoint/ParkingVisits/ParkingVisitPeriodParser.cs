using System.Globalization;

namespace ParkingBusinessResult.Api.Endpoint.ParkingVisits;

internal sealed record ParkingVisitPeriod(DateTime From, DateTime ToExclusive, string Label, bool IsMonthly);

internal static class ParkingVisitPeriodParser
{
    public static ParkingVisitPeriod? Parse(string? reportingPeriod, string? fromDate, string? toDate)
    {
        if (!string.IsNullOrWhiteSpace(reportingPeriod) && fromDate is null && toDate is null &&
            DateTime.TryParseExact(reportingPeriod, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime month) &&
            (month.Year != 9999 || month.Month != 12))
            return new(month, month.AddMonths(1), reportingPeriod, true);

        if (reportingPeriod is null &&
            DateTime.TryParseExact(fromDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime start) &&
            DateTime.TryParseExact(toDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime end) &&
            start <= end && end.Date < DateTime.MaxValue.Date)
            return new(start, end.AddDays(1), $"{fromDate}/{toDate}", false);

        return null;
    }
}
