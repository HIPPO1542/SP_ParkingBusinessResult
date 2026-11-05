using System.Security.Claims;
using ParkingBusinessResult.Application.DTOs;
using ParkingBusinessResult.Application.Features.BusinessResults.Commands.Create;

namespace ParkingBusinessResult.Api.Endpoint.BusinessResults;

public static class CreateBusinessResultEndpoint
{
    public static void Map(RouteGroupBuilder routes) => routes.MapPost("/", Handle);

    private static IResult Handle(CreateBusinessResultRequest request, ClaimsPrincipal user, CreateBusinessResultHandler handler)
    {
        if (request.ParkingLotId is null || request.ReportingPeriod is null || request.Revenue is null ||
            request.CompletedVehicleCount is null || request.AdditionalFields is { Count: > 0 })
            return EndpointSupport.InvalidBody();

        return EndpointSupport.Respond(handler.Handle(new CreateBusinessResultCommand(request.ParkingLotId.Value, request.ReportingPeriod,
            request.Revenue.Value, request.CompletedVehicleCount.Value, request.Note, EndpointSupport.GetAccess(user))));
    }
}
