using System.Security.Claims;
using ParkingBusinessResult.Api.Contracts;
using ParkingBusinessResult.Application.Features.ParkingBookings.Queries.Filters;

namespace ParkingBusinessResult.Api.Endpoints.ParkingBookings;

public static class GetParkingBookingFiltersEndpoint
{
    public static void Map(RouteGroupBuilder routes) => routes.MapGet("/filters", Handle);

    private static IResult Handle(ClaimsPrincipal user, GetParkingBookingFiltersHandler handler)
    {
        ParkingBookingFilters filters = handler.Handle(new GetParkingBookingFiltersQuery(EndpointSupport.GetAccess(user)));
        return Results.Ok(ApiResponse<ParkingBookingFilters>.Success(filters));
    }
}
