using System.Security.Claims;
using ParkingBusinessResult.Api.Contracts;
using ParkingBusinessResult.Application.Features.ParkingBookings.Queries.CompareTariff;

namespace ParkingBusinessResult.Api.Endpoints.ParkingBookings;

public static class CompareBookingTariffEndpoint
{
    public static void Map(RouteGroupBuilder routes) => routes.MapGet("/{id:guid}/tariff-comparison", Handle);

    private static IResult Handle(Guid id, string? dayType, HttpRequest request, ClaimsPrincipal user, CompareBookingTariffHandler handler)
    {
        if (dayType is not ("WEEKDAY" or "WEEKEND" or "HOLIDAY") || request.Query.Keys.Any(key => !string.Equals(key, "dayType", StringComparison.OrdinalIgnoreCase)))
            return Results.Ok(ApiResponse<object>.BusinessError("Cần chọn dayType là WEEKDAY, WEEKEND hoặc HOLIDAY."));
        TariffComparison comparison = handler.Handle(new CompareBookingTariffQuery(id, dayType, EndpointSupport.GetAccess(user)));
        return Results.Ok(ApiResponse<TariffComparison>.Success(comparison));
    }
}
