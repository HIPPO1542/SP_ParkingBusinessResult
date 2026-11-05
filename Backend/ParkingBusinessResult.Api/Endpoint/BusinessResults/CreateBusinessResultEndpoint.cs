using System.Security.Claims;
using ParkingBusinessResult.Application.DTOs;
using ParkingBusinessResult.Application.Features.BusinessResults.Commands.Create;

namespace ParkingBusinessResult.Api.Endpoint.BusinessResults;

public static class CreateBusinessResultEndpoint
{
    public static void Map(RouteGroupBuilder routes) => routes.MapPost("/", Handle);

    private static IResult Handle(CreateBusinessResultRequest request, ClaimsPrincipal user, CreateBusinessResultHandler handler) =>
        EndpointSupport.Respond(handler.Handle(new CreateBusinessResultCommand(request.ParkingLotId, request.ReportingPeriod,
            request.Revenue, request.CompletedVehicleCount, request.Note, EndpointSupport.GetAccess(user))));
}
