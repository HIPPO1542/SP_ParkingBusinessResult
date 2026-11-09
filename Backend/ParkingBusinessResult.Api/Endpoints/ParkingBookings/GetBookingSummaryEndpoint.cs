using System.Security.Claims;
using ParkingBusinessResult.Api.Contracts;
using ParkingBusinessResult.Application.Features.ParkingBookings.Queries.Summary;

namespace ParkingBusinessResult.Api.Endpoints.ParkingBookings;

public static class GetBookingSummaryEndpoint
{
    public static void Map(RouteGroupBuilder routes) => routes.MapGet("/summary", Handle);

    private static IResult Handle(string? reportingPeriod, string? fromDate, string? toDate, Guid? parkingLotId, string? district, string? vehicleType,
        HttpRequest request, ClaimsPrincipal user, GetBookingSummaryHandler handler)
    {
        string[] allowed = ["reportingPeriod", "fromDate", "toDate", "parkingLotId", "parkingLotIds", "district", "vehicleType"];
        if (request.Query.Keys.Any(key => !allowed.Contains(key, StringComparer.OrdinalIgnoreCase)))
            return Results.Ok(ApiResponse<object>.BusinessError("Bộ lọc chưa được hỗ trợ."));
        if (vehicleType is not null && (string.IsNullOrWhiteSpace(vehicleType) || vehicleType.Length > 100))
            return Results.Ok(ApiResponse<object>.BusinessError("Loại xe không hợp lệ."));
        BookingPeriod? period = BookingPeriodParser.Parse(reportingPeriod, fromDate, toDate);
        if (period is null)
            return Results.Ok(ApiResponse<object>.BusinessError("Cung cấp kỳ yyyy-MM hoặc khoảng fromDate và toDate hợp lệ (yyyy-MM-dd)."));
        var parkingLotIds = new HashSet<Guid>();
        foreach (string? id in request.Query["parkingLotIds"])
        {
            if (!Guid.TryParse(id, out Guid parsed) || parsed == Guid.Empty)
                return Results.Ok(ApiResponse<object>.BusinessError("Danh sách bãi xe không hợp lệ."));
            parkingLotIds.Add(parsed);
        }
        BookingSummary summary = handler.Handle(new GetBookingSummaryQuery(period.From, period.ToExclusive, period.Label, parkingLotId, EndpointSupport.GetAccess(user), period.IsMonthly, district, parkingLotIds.Count == 0 ? null : parkingLotIds, vehicleType?.Trim()));
        return Results.Ok(ApiResponse<BookingSummary>.Success(summary));
    }
}
