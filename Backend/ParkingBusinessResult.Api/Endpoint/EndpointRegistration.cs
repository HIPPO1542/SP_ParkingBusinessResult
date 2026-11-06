using ParkingBusinessResult.Api.Endpoint.BusinessResults;
using ParkingBusinessResult.Api.Endpoint.BusinessReports;
using ParkingBusinessResult.Api.Endpoint.LocalAccounts;
using ParkingBusinessResult.Api.Endpoint.ParkingBookings;

namespace ParkingBusinessResult.Api.Endpoint;

public static class EndpointRegistration
{
    public static void MapApplicationEndpoints(this WebApplication app)
    {
        if ((app.Configuration["Authentication:Mode"] ?? "Local") == "Local")
        {
            app.MapGet("/", () =>
            {
                using Stream stream = typeof(EndpointRegistration).Assembly.GetManifestResourceStream("ParkingBusinessResult.Api.Frontend.index.html")!;
                using var reader = new StreamReader(stream);
                return Results.Content(reader.ReadToEnd(), "text/html; charset=utf-8");
            }).AllowAnonymous();
        }
        RouteGroupBuilder businessResults = app.MapGroup("/gdci/parking/parkingLot/businessOperation").RequireAuthorization("BusinessResultAccess");
        ListBusinessResultsEndpoint.Map(businessResults);
        GetBusinessResultEndpoint.Map(businessResults);
        CreateBusinessResultEndpoint.Map(businessResults);
        UpdateBusinessResultEndpoint.Map(businessResults);
        DeleteBusinessResultEndpoint.Map(businessResults);
        ImportParkingBookingsEndpoint.Map(businessResults);

        RouteGroupBuilder businessReports = app.MapGroup("/gdci/parking/parkingLot/parkingBusinessReport").RequireAuthorization("BusinessResultAccess");
        ListBusinessReportEndpoint.Map(businessReports);
        GetBusinessReportSummaryEndpoint.Map(businessReports);
        ExportBusinessReportEndpoint.Map(businessReports);

        DownloadBusinessReportEndpoint.Map(app);

        RouteGroupBuilder parkingBookings = app.MapGroup("/gdci/parking/parkingLot/parkingBooking").RequireAuthorization("BusinessResultAccess");
        ListParkingBookingsEndpoint.Map(parkingBookings);
        GetParkingBookingFiltersEndpoint.Map(parkingBookings);
        GetParkingBookingSummaryEndpoint.Map(parkingBookings);
        ExportParkingBookingReportEndpoint.Map(parkingBookings);
        CompareParkingBookingTariffEndpoint.Map(parkingBookings);

        if ((app.Configuration["Authentication:Mode"] ?? "Local") == "Local")
        {
            RouteGroupBuilder accounts = app.MapGroup("/auth");
            LoginEndpoint.Map(accounts);
            CreateAccountEndpoint.Map(accounts);
        }
    }
}
