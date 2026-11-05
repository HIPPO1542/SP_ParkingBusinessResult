using ParkingBusinessResult.Api.Endpoint.BusinessResults;
using ParkingBusinessResult.Api.Endpoint.BusinessReports;
using ParkingBusinessResult.Api.Endpoint.DevelopmentAccounts;
using ParkingBusinessResult.Api.Endpoint.ParkingVisits;

namespace ParkingBusinessResult.Api.Endpoint;

public static class EndpointRegistration
{
    public static void MapDevelopmentEndpoints(this WebApplication app)
    {
        RouteGroupBuilder businessResults = app.MapGroup("/gdci/parking/parkingLot/businessOperation").RequireAuthorization("BusinessResultAccess");
        ListBusinessResultsEndpoint.Map(businessResults);
        GetBusinessResultEndpoint.Map(businessResults);
        CreateBusinessResultEndpoint.Map(businessResults);
        UpdateBusinessResultEndpoint.Map(businessResults);
        DeleteBusinessResultEndpoint.Map(businessResults);
        ImportParkingVisitsEndpoint.Map(businessResults);

        RouteGroupBuilder businessReports = app.MapGroup("/gdci/parking/parkingLot/parkingBusinessReport").RequireAuthorization("BusinessResultAccess");
        ListBusinessReportEndpoint.Map(businessReports);
        GetBusinessReportSummaryEndpoint.Map(businessReports);
        ExportBusinessReportEndpoint.Map(businessReports);

        RouteGroupBuilder parkingVisits = app.MapGroup("/gdci/parking/parkingLot/parkingVisit").RequireAuthorization("BusinessResultAccess");
        ListParkingVisitsEndpoint.Map(parkingVisits);
        GetParkingVisitSummaryEndpoint.Map(parkingVisits);
        ExportParkingVisitReportEndpoint.Map(parkingVisits);
        CompareParkingVisitTariffEndpoint.Map(parkingVisits);

        RouteGroupBuilder accounts = app.MapGroup("/dev/auth");
        LoginEndpoint.Map(accounts);
        CreateAccountEndpoint.Map(accounts);
    }
}
