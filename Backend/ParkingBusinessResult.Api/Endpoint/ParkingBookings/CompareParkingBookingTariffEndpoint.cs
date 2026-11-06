using System.Security.Claims;
using ParkingBusinessResult.Api.Contracts;
using ParkingBusinessResult.Application.Features.ParkingBookings.Queries.CompareTariff;

namespace ParkingBusinessResult.Api.Endpoint.ParkingBookings;

public static class CompareParkingBookingTariffEndpoint
{
    public static void Map(RouteGroupBuilder routes) => routes.MapGet("/{id:guid}/tariff-comparison", Handle);

    private static IResult Handle(Guid id, string? dayType, HttpRequest request, ClaimsPrincipal user, CompareParkingBookingTariffHandler handler)
    {
        if (dayType is not ("WEEKDAY" or "WEEKEND" or "HOLIDAY") || request.Query.Keys.Any(key => !string.Equals(key, "dayType", StringComparison.OrdinalIgnoreCase)))
            return Results.Ok(ApiResponse<object>.BusinessError("Cần chọn dayType là WEEKDAY, WEEKEND hoặc HOLIDAY."));
        TariffComparison comparison = handler.Handle(new CompareParkingBookingTariffQuery(id, dayType, EndpointSupport.GetAccess(user)));
        return Results.Ok(ApiResponse<TariffComparison>.Success(comparison));
    }
}
