using ParkingBusinessResult.Application.Features.DevelopmentAccounts.Common;
using ParkingBusinessResult.Application.Features.BusinessReports.Common;
using ParkingBusinessResult.Application.Features.ParkingVisits.Common;
using ParkingBusinessResult.Api.Endpoint;
using ParkingBusinessResult.Application.Features.DevelopmentAccounts.Commands.Bootstrap;
using ParkingBusinessResult.Domain;
using ParkingBusinessResult.Infrastructure;
using ParkingBusinessResult.Infrastructure.Repositories;

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
        builder.Services.AddSingleton<IBusinessReportParkingLotSearch>(new SqliteBusinessReportParkingLotSearch(databasePath));
        builder.Services.AddSingleton<IParkingVisitSpreadsheetReader, ExcelParkingVisitSpreadsheetReader>();
        var visitRepository = new SqliteParkingVisitImportRepository(databasePath);
        builder.Services.AddSingleton<IParkingVisitImportRepository>(visitRepository);
        builder.Services.AddSingleton<IParkingVisitSummaryRepository>(visitRepository);
        builder.Services.AddSingleton<IParkingVisitListRepository>(visitRepository);
        builder.Services.AddSingleton<IParkingVisitTariffRepository>(new SqliteParkingVisitTariffRepository(databasePath));
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
