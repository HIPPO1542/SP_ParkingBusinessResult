using System.Security.Claims;
using ParkingBusinessResult.Application.Features.ParkingBookings.Commands.CheckIn;

namespace ParkingBusinessResult.Api.Endpoints.ParkingBookings;

public static class CheckInEndpoint
{
    public static void Map(RouteGroupBuilder routes) => routes.MapPost("/{id:guid}/check-in", Handle);

    private static IResult Handle(Guid id, ClaimsPrincipal user, CheckInHandler handler) =>
        EndpointSupport.Respond(handler.Handle(new CheckInCommand(id, EndpointSupport.GetAccess(user), EndpointSupport.GetAuditActor(user))));
}
