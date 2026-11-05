using System.Security.Claims;
using ParkingBusinessResult.Api.Contracts;
using ParkingBusinessResult.Application.Features.ParkingVisits.Queries.Filters;

namespace ParkingBusinessResult.Api.Endpoint.ParkingVisits;

public static class GetParkingVisitFiltersEndpoint
{
    public static void Map(RouteGroupBuilder routes) => routes.MapGet("/filters", Handle);

    private static IResult Handle(ClaimsPrincipal user, GetParkingVisitFiltersHandler handler)
    {
        ParkingVisitFilters filters = handler.Handle(new GetParkingVisitFiltersQuery(EndpointSupport.GetAccess(user)));
        return Results.Ok(ApiResponse<ParkingVisitFilters>.Success(filters));
    }
}
