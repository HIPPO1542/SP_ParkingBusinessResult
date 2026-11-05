using System.Security.Claims;
using ParkingBusinessResult.Application.Features.BusinessResults.Queries.GetById;

namespace ParkingBusinessResult.Api.Endpoint.BusinessResults;

public static class GetBusinessResultEndpoint
{
    public static void Map(RouteGroupBuilder routes) => routes.MapGet("/{id:guid}", Handle);

    private static IResult Handle(Guid id, ClaimsPrincipal user, GetBusinessResultHandler handler) =>
        EndpointSupport.Respond(handler.Handle(new GetBusinessResultQuery(id, EndpointSupport.GetAccess(user))));
}
