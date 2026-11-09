using System.Globalization;
using ParkingBusinessResult.Api.Contracts;
using ParkingBusinessResult.Application.Features.ParkingBookings.Common;
using ParkingBusinessResult.Application.Features.ParkingBookings.Queries.Estimate;

namespace ParkingBusinessResult.Api.Endpoints.ParkingBookings;

public static class GetBookingEstimateEndpoint
{
    public static void Map(RouteGroupBuilder routes) => routes.MapGet("/estimate", Handle);

    private static IResult Handle(Guid parkingLotId, string? vehicleType, string? expectedArrivalAt, string? expectedDepartureAt,
        HttpRequest request, GetBookingEstimateHandler handler)
    {
        string[] allowed = ["parkingLotId", "vehicleType", "expectedArrivalAt", "expectedDepartureAt"];
        string[] formats = ["yyyy-MM-ddTHH:mm", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-dd HH:mm:ss"];
        if (request.Query.Keys.Any(key => !allowed.Contains(key, StringComparer.OrdinalIgnoreCase)) || vehicleType is null ||
            !DateTime.TryParseExact(expectedArrivalAt, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime start) ||
            !DateTime.TryParseExact(expectedDepartureAt, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime end))
            return EndpointSupport.InvalidBody();
        BookingEstimateResult result = handler.Handle(new GetBookingEstimateQuery(new(parkingLotId, vehicleType.Trim(), start, end)));
        return result.Error is null ? Results.Ok(ApiResponse<BookingEstimate>.Success(result.Estimate!)) :
            Results.Ok(ApiResponse<BookingEstimate>.BusinessError(result.Error));
    }
}
