using System.Globalization;
using System.Security.Claims;
using ParkingBusinessResult.Api.Contracts;
using ParkingBusinessResult.Application.Features.ParkingVisits.Queries.Summary;

namespace ParkingBusinessResult.Api.Endpoint.ParkingVisits;

public static class GetParkingVisitSummaryEndpoint
{
    public static void Map(RouteGroupBuilder routes) => routes.MapGet("/summary", Handle);

    private static IResult Handle(string? reportingPeriod, string? fromDate, string? toDate, Guid? parkingLotId, string? district,
        HttpRequest request, ClaimsPrincipal user, GetParkingVisitSummaryHandler handler)
    {
        string[] allowed = ["reportingPeriod", "fromDate", "toDate", "parkingLotId", "district"];
        if (request.Query.Keys.Any(key => !allowed.Contains(key, StringComparer.OrdinalIgnoreCase)))
            return Results.Ok(ApiResponse<object>.BusinessError("Bộ lọc chưa được hỗ trợ."));
        DateTime from;
        DateTime toExclusive;
        string label;
        bool isMonthly;
        if (!string.IsNullOrWhiteSpace(reportingPeriod) && fromDate is null && toDate is null &&
            DateTime.TryParseExact(reportingPeriod, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime month) &&
            (month.Year != 9999 || month.Month != 12))
        {
            from = month;
            toExclusive = month.AddMonths(1);
            label = reportingPeriod;
            isMonthly = true;
        }
        else if (reportingPeriod is null &&
            DateTime.TryParseExact(fromDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime start) &&
            DateTime.TryParseExact(toDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime end) &&
            start <= end && end.Date < DateTime.MaxValue.Date)
        {
            from = start;
            toExclusive = end.AddDays(1);
            label = $"{fromDate}/{toDate}";
            isMonthly = false;
        }
        else
        {
            return Results.Ok(ApiResponse<object>.BusinessError("Cung cấp kỳ yyyy-MM hoặc khoảng fromDate và toDate hợp lệ (yyyy-MM-dd)."));
        }
        ParkingVisitSummary summary = handler.Handle(new GetParkingVisitSummaryQuery(from, toExclusive, label, parkingLotId, EndpointSupport.GetAccess(user), isMonthly, district));
        return Results.Ok(ApiResponse<ParkingVisitSummary>.Success(summary));
    }
}
