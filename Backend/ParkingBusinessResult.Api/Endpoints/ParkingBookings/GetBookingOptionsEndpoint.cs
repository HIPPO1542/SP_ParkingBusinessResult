using System.Security.Claims;
using ParkingBusinessResult.Api.Contracts;
using ParkingBusinessResult.Application.Features.ParkingBookings.Common;
using ParkingBusinessResult.Application.Features.ParkingBookings.Queries.Options;

namespace ParkingBusinessResult.Api.Endpoints.ParkingBookings;

public static class GetBookingOptionsEndpoint
{
    public static void Map(RouteGroupBuilder routes) => routes.MapGet("/options", Handle);

    private static IResult Handle(ClaimsPrincipal user, GetBookingOptionsHandler handler) =>
        Results.Ok(ApiResponse<BookingOptions>.Success(handler.Handle(new GetBookingOptionsQuery(
            user.IsInRole("ROLE_CUSTOMER") ? EndpointSupport.GetAuditActor(user).Id : null))));
}
