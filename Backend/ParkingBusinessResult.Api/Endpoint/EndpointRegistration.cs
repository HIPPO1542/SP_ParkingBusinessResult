using ParkingBusinessResult.Api.Endpoint.BusinessResults;
using ParkingBusinessResult.Api.Endpoint.DevelopmentAccounts;

namespace ParkingBusinessResult.Api.Endpoint;

public static class EndpointRegistration
{
    public static void MapDevelopmentEndpoints(this WebApplication app)
    {
        RouteGroupBuilder businessResults = app.MapGroup("/dev/business-results").RequireAuthorization("BusinessResultAccess");
        ListBusinessResultsEndpoint.Map(businessResults);
        GetBusinessResultEndpoint.Map(businessResults);
        CreateBusinessResultEndpoint.Map(businessResults);
        UpdateBusinessResultEndpoint.Map(businessResults);
        DeleteBusinessResultEndpoint.Map(businessResults);

        RouteGroupBuilder accounts = app.MapGroup("/dev/auth");
        LoginEndpoint.Map(accounts);
        CreateAccountEndpoint.Map(accounts);
    }
}
