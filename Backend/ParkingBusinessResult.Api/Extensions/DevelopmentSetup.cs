using ParkingBusinessResult.Application.Features.DevelopmentAccounts.Common;
using ParkingBusinessResult.Application.Features.BusinessReports.Common;
using ParkingBusinessResult.Application.Features.BusinessReports.Queries.List;
using ParkingBusinessResult.Application.Features.ParkingBookings.Common;
using ParkingBusinessResult.Api.Endpoint;
using ParkingBusinessResult.Application.Features.DevelopmentAccounts.Commands.Bootstrap;
using ParkingBusinessResult.Domain;
using ParkingBusinessResult.Infrastructure;
using ParkingBusinessResult.Infrastructure.Repositories;
using ParkingBusinessResult.Infrastructure.Reports;

namespace ParkingBusinessResult.Api.Extensions;

public static class DevelopmentSetup
{
    public static void Configure(WebApplicationBuilder builder)
    {
        DevelopmentAuthentication.Configure(builder);
        builder.Services.AddBusinessResultHandlers();

        string databasePath = builder.Configuration["ParkingBusinessResult:DatabasePath"]
            ?? Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "../../database/sqlite/parking_business_result.db"));
        builder.Services.AddSingleton<IBusinessResultRepository>(new SqliteBusinessResultRepository(databasePath));
        string exportDirectory = builder.Configuration["ParkingBusinessResult:ExportDirectory"]
            ?? Path.Combine(builder.Environment.ContentRootPath, "App_Data", "exports");
        builder.Services.AddSingleton(new BusinessReportExportStore(databasePath, Path.GetFullPath(exportDirectory)));
        builder.Services.AddSingleton<IBusinessReportParkingLotSearch>(new SqliteBusinessReportParkingLotSearch(databasePath));
        builder.Services.AddSingleton<IBusinessReportMetadataReader>(new SqliteBusinessReportMetadataReader(databasePath));
        builder.Services.AddSingleton<IParkingBookingSpreadsheetReader, ExcelParkingBookingSpreadsheetReader>();
        var visitRepository = new SqliteParkingBookingImportRepository(databasePath);
        builder.Services.AddSingleton<IParkingBookingImportRepository>(visitRepository);
        builder.Services.AddSingleton<IParkingBookingSummaryRepository>(visitRepository);
        builder.Services.AddSingleton<IParkingBookingListRepository>(visitRepository);
        builder.Services.AddSingleton<IParkingBookingTariffRepository>(new SqliteParkingBookingTariffRepository(databasePath));
        builder.Services.AddSingleton<IDevelopmentAccountRepository>(new SqliteDevelopmentAccountRepository(databasePath));
        builder.Services.AddSingleton<IDevelopmentCredentials, DevelopmentCredentials>();
    }

    public static void Map(WebApplication app)
    {
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseRateLimiter();

        var command = new BootstrapAdministratorCommand(
            app.Configuration["Authentication:BootstrapUsername"],
            app.Configuration["Authentication:BootstrapPassword"]);
        app.Services.GetRequiredService<BootstrapAdministratorHandler>().Handle(command);
        app.MapDevelopmentEndpoints();
    }
}
