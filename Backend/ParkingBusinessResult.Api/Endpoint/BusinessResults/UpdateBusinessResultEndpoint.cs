using System.Security.Claims;
using ParkingBusinessResult.Application.DTOs;
using ParkingBusinessResult.Application.Features.BusinessResults.Commands.Update;

namespace ParkingBusinessResult.Api.Endpoint.BusinessResults;

public static class UpdateBusinessResultEndpoint
{
    public static void Map(RouteGroupBuilder routes) => routes.MapPut("/{id:guid}", Handle);

    private static IResult Handle(Guid id, UpdateBusinessResultRequest request, ClaimsPrincipal user, UpdateBusinessResultHandler handler) =>
        EndpointSupport.Respond(handler.Handle(new UpdateBusinessResultCommand(id, request.Revenue, request.CompletedVehicleCount,
            request.Note, EndpointSupport.GetAccess(user))));
}
