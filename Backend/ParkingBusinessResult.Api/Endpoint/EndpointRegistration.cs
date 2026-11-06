using ParkingBusinessResult.Api.Endpoint.BusinessResults;
using ParkingBusinessResult.Api.Endpoint.BusinessReports;
using ParkingBusinessResult.Api.Endpoint.DevelopmentAccounts;
using ParkingBusinessResult.Api.Endpoint.ParkingBookings;
using ParkingBusinessResult.Infrastructure.Reports;
using System.Security.Claims;

namespace ParkingBusinessResult.Api.Endpoint;

public static class EndpointRegistration
{
    public static void MapDevelopmentEndpoints(this WebApplication app)
    {
        app.MapGet("/", () =>
        {
            using Stream stream = typeof(EndpointRegistration).Assembly.GetManifestResourceStream("ParkingBusinessResult.Api.Frontend.index.html")!;
            using var reader = new StreamReader(stream);
            return Results.Content(reader.ReadToEnd(), "text/html; charset=utf-8");
        }).AllowAnonymous();
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

        app.MapGet("/static/exports/{fileName}", (string fileName, ClaimsPrincipal user, BusinessReportExportStore store) =>
        {
            if (!Guid.TryParse(user.FindFirst("sub")?.Value, out Guid userId)) return Results.NotFound();
            string? path = store.FindFile(userId, fileName);
            if (path is null) return Results.NotFound();
            string contentType = fileName.EndsWith(".pdf", StringComparison.Ordinal)
                ? "application/pdf" : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            return Results.File(path, contentType, fileName);
        }).RequireAuthorization("BusinessResultAccess");

        RouteGroupBuilder parkingBookings = app.MapGroup("/gdci/parking/parkingLot/parkingBooking").RequireAuthorization("BusinessResultAccess");
        ListParkingBookingsEndpoint.Map(parkingBookings);
        GetParkingBookingFiltersEndpoint.Map(parkingBookings);
        GetParkingBookingSummaryEndpoint.Map(parkingBookings);
        ExportParkingBookingReportEndpoint.Map(parkingBookings);
        CompareParkingBookingTariffEndpoint.Map(parkingBookings);

        RouteGroupBuilder accounts = app.MapGroup("/dev/auth");
        LoginEndpoint.Map(accounts);
        CreateAccountEndpoint.Map(accounts);
    }
}
