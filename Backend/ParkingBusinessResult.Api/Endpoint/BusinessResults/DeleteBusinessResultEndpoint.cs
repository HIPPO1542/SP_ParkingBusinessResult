using System.Security.Claims;
using ParkingBusinessResult.Application.Features.BusinessResults.Commands.Delete;

namespace ParkingBusinessResult.Api.Endpoint.BusinessResults;

public static class DeleteBusinessResultEndpoint
{
    public static void Map(RouteGroupBuilder routes) => routes.MapDelete("/{id:guid}", Handle);

    private static IResult Handle(Guid id, ClaimsPrincipal user, DeleteBusinessResultHandler handler) =>
        EndpointSupport.Respond(handler.Handle(new DeleteBusinessResultCommand(id, EndpointSupport.GetAccess(user))));
}
