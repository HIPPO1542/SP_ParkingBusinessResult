using System.Security.Claims;
using ParkingBusinessResult.Api.Contracts;
using ParkingBusinessResult.Application.Features.ParkingVisits.Queries.CompareTariff;

namespace ParkingBusinessResult.Api.Endpoint.ParkingVisits;

public static class CompareParkingVisitTariffEndpoint
{
    public static void Map(RouteGroupBuilder routes) => routes.MapGet("/{id:guid}/tariff-comparison", Handle);

    private static IResult Handle(Guid id, string? dayType, HttpRequest request, ClaimsPrincipal user, CompareParkingVisitTariffHandler handler)
    {
        if (dayType is not ("WEEKDAY" or "WEEKEND" or "HOLIDAY") || request.Query.Keys.Any(key => !string.Equals(key, "dayType", StringComparison.OrdinalIgnoreCase)))
            return Results.Ok(ApiResponse<object>.BusinessError("Cần chọn dayType là WEEKDAY, WEEKEND hoặc HOLIDAY."));
        TariffComparison comparison = handler.Handle(new CompareParkingVisitTariffQuery(id, dayType, EndpointSupport.GetAccess(user)));
        return Results.Ok(ApiResponse<TariffComparison>.Success(comparison));
    }
}
