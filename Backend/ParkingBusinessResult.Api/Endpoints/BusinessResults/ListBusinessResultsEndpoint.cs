using System.Security.Claims;
using ParkingBusinessResult.Api.Contracts;
using ParkingBusinessResult.Application.Features.BusinessResults.Queries.List;
using ParkingBusinessResult.Domain;

namespace ParkingBusinessResult.Api.Endpoints.BusinessResults;

public static class ListBusinessResultsEndpoint
{
    public static void Map(RouteGroupBuilder routes) => routes.MapGet("/", Handle);

    private static IResult Handle(ClaimsPrincipal user, ListBusinessResultsHandler handler)
    {
        IReadOnlyList<BusinessResultRecord> results = handler.Handle(new ListBusinessResultsQuery(EndpointSupport.GetAccess(user)));
        return Results.Ok(ApiResponse<IReadOnlyList<BusinessResultRecord>>.Success(results));
    }
}
