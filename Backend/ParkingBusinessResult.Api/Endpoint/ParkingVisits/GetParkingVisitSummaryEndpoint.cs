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
        ParkingVisitPeriod? period = ParkingVisitPeriodParser.Parse(reportingPeriod, fromDate, toDate);
        if (period is null)
            return Results.Ok(ApiResponse<object>.BusinessError("Cung cấp kỳ yyyy-MM hoặc khoảng fromDate và toDate hợp lệ (yyyy-MM-dd)."));
        ParkingVisitSummary summary = handler.Handle(new GetParkingVisitSummaryQuery(period.From, period.ToExclusive, period.Label, parkingLotId, EndpointSupport.GetAccess(user), period.IsMonthly, district));
        return Results.Ok(ApiResponse<ParkingVisitSummary>.Success(summary));
    }
}
