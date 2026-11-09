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
        ImportBookingsEndpoint.Map(businessResults);

        RouteGroupBuilder businessReports = app.MapGroup("/gdci/parking/parkingLot/parkingBusinessReport").RequireAuthorization("BusinessResultAccess");
        ListReportsEndpoint.Map(businessReports);
        GetReportSummaryEndpoint.Map(businessReports);
        ExportReportEndpoint.Map(businessReports);

        DownloadReportEndpoint.Map(app);

        RouteGroupBuilder parkingBookings = app.MapGroup("/gdci/parking/parkingLot/parkingBooking").RequireAuthorization("BusinessResultAccess");
        ListBookingsEndpoint.Map(parkingBookings);
        GetBookingFiltersEndpoint.Map(parkingBookings);
        GetBookingSummaryEndpoint.Map(parkingBookings);
        ExportBookingReportEndpoint.Map(parkingBookings);
        CompareBookingTariffEndpoint.Map(parkingBookings);

        RouteGroupBuilder reservations = app.MapGroup("/gdci/parking/parkingLot/parkingReservation").RequireAuthorization("CustomerBookingAccess");
        ReserveBookingEndpoint.Map(reservations);
        GetMyBookingsEndpoint.Map(reservations);
        RouteGroupBuilder catalog = app.MapGroup("/gdci/parking/parkingLot/parkingReservation").RequireAuthorization("BookingCatalogAccess");
        GetBookingOptionsEndpoint.Map(catalog);
        RouteGroupBuilder staffReservations = app.MapGroup("/gdci/parking/parkingLot/parkingReservation").RequireAuthorization("BusinessResultAccess");
        CheckInEndpoint.Map(staffReservations);
        CheckOutEndpoint.Map(staffReservations);

        if ((app.Configuration["Authentication:Mode"] ?? "Local") == "Local")
        {
            RouteGroupBuilder accounts = app.MapGroup("/auth");
            LoginEndpoint.Map(accounts);
            CreateAccountEndpoint.Map(accounts);
            RegisterCustomerEndpoint.Map(accounts);
        }
    }
}
