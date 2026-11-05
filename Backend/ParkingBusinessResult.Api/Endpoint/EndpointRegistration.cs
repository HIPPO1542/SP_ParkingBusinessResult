using ParkingBusinessResult.Api.Endpoint.BusinessResults;
using ParkingBusinessResult.Api.Endpoint.BusinessReports;
using ParkingBusinessResult.Api.Endpoint.DevelopmentAccounts;
using ParkingBusinessResult.Api.Endpoint.ParkingVisits;

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

        RouteGroupBuilder businessReports = app.MapGroup("/dev/business-reports").RequireAuthorization("BusinessResultAccess");
        ListBusinessReportEndpoint.Map(businessReports);
        GetBusinessReportSummaryEndpoint.Map(businessReports);
        ExportBusinessReportEndpoint.Map(businessReports);

        RouteGroupBuilder parkingVisits = app.MapGroup("/dev/parking-visits").RequireAuthorization("BusinessResultAccess");
        ImportParkingVisitsEndpoint.Map(parkingVisits);
        GetParkingVisitSummaryEndpoint.Map(parkingVisits);
        ExportParkingVisitReportEndpoint.Map(parkingVisits);

        RouteGroupBuilder accounts = app.MapGroup("/dev/auth");
        LoginEndpoint.Map(accounts);
        CreateAccountEndpoint.Map(accounts);
    }
}
