using ParkingBusinessResult.Api.Endpoints.BusinessResults;
using ParkingBusinessResult.Api.Endpoints.BusinessReports;
using ParkingBusinessResult.Api.Endpoints.LocalAccounts;
using ParkingBusinessResult.Api.Endpoints.ParkingBookings;

namespace ParkingBusinessResult.Api.Endpoints;

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
