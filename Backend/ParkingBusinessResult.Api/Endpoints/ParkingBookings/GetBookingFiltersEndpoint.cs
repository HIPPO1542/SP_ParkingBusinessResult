using System.Security.Claims;
using ParkingBusinessResult.Api.Contracts;
using ParkingBusinessResult.Application.Features.ParkingBookings.Queries.Filters;

namespace ParkingBusinessResult.Api.Endpoints.ParkingBookings;

public static class GetBookingFiltersEndpoint
{
    public static void Map(RouteGroupBuilder routes) => routes.MapGet("/filters", Handle);

    private static IResult Handle(ClaimsPrincipal user, GetBookingFiltersHandler handler)
    {
        BookingFilters filters = handler.Handle(new GetBookingFiltersQuery(EndpointSupport.GetAccess(user)));
        return Results.Ok(ApiResponse<BookingFilters>.Success(filters));
    }
}
