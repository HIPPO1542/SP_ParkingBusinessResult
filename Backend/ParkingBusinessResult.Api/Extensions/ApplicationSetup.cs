using ParkingBusinessResult.Application.Features.LocalAccounts.Common;
using ParkingBusinessResult.Application.Features.BusinessReports.Common;
using ParkingBusinessResult.Application.Features.BusinessReports.Queries.List;
using ParkingBusinessResult.Application.Features.ParkingBookings.Common;
using ParkingBusinessResult.Api.Endpoint;
using ParkingBusinessResult.Application.Features.LocalAccounts.Commands.Bootstrap;
using ParkingBusinessResult.Domain;
using ParkingBusinessResult.Infrastructure;
using ParkingBusinessResult.Infrastructure.Repositories;
using ParkingBusinessResult.Infrastructure.Reports;
using ParkingBusinessResult.Infrastructure.Data;
using ParkingBusinessResult.Api.Middlewares;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ParkingBusinessResult.Api.Extensions;

public static class ApplicationSetup
{
    public static void Configure(WebApplicationBuilder builder)
    {
        AuthenticationSetup.Configure(builder);
        builder.Services.AddBusinessResultHandlers();
        builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);
        builder.Services.Configure<FormOptions>(options => options.MultipartBodyLengthLimit = 10 * 1024 * 1024);
        builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 10 * 1024 * 1024 + 65536);

        string? configuredDatabase = builder.Configuration["ParkingBusinessResult:DatabasePath"];
        string databasePath = Path.GetFullPath(configuredDatabase ?? "../../database/sqlite/parking_business_result.db", builder.Environment.ContentRootPath);
        if (!File.Exists(databasePath)) throw new InvalidOperationException("The configured database does not exist. Apply reviewed migrations before starting the application.");
        builder.Services.AddSingleton<IBusinessResultRepository>(new SqliteBusinessResultRepository(databasePath));
        string? configuredExports = builder.Configuration["ParkingBusinessResult:ExportDirectory"];
        string exportDirectory = Path.GetFullPath(configuredExports ?? "App_Data/exports", builder.Environment.ContentRootPath);
        builder.Services.AddSingleton<IBusinessReportExportStore>(new BusinessReportExportStore(exportDirectory, builder.Configuration.GetValue<bool>("ParkingBusinessResult:AllowLegacyReportDownloads")));
        builder.Services.AddSingleton<IBusinessReportDocumentWriter, BusinessReportDocumentWriter>();
        builder.Services.AddSingleton<IBusinessReportParkingLotSearch>(new SqliteBusinessReportParkingLotSearch(databasePath));
        builder.Services.AddSingleton<IBusinessReportMetadataReader>(new SqliteBusinessReportMetadataReader(databasePath));
        builder.Services.AddSingleton<IParkingBookingSpreadsheetReader, ExcelParkingBookingSpreadsheetReader>();
        builder.Services.AddSingleton<IParkingBookingImportRepository>(new SqliteParkingBookingImportRepository(databasePath));
        var bookingQueries = new SqliteParkingBookingQueryRepository(databasePath);
        builder.Services.AddSingleton<IParkingBookingSummaryRepository>(bookingQueries);
        builder.Services.AddSingleton<IParkingBookingListRepository>(bookingQueries);
        builder.Services.AddSingleton<IParkingBookingTariffRepository>(new SqliteParkingBookingTariffRepository(databasePath));
        builder.Services.AddSingleton<ILocalAccountRepository>(new SqliteLocalAccountRepository(databasePath));
        builder.Services.AddSingleton<ILocalCredentials, LocalCredentials>();
        var probe = new SqliteStorageProbe(databasePath);
        builder.Services.AddHealthChecks().AddCheck("sqlite", () => probe.IsReady()
            ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy("Required storage is unavailable."));
    }

    public static void Map(WebApplication app)
    {
        app.UseMiddleware<ApiExceptionMiddleware>();
        app.UseStatusCodePages(async context =>
        {
            HttpContext http = context.HttpContext;
            if (!http.Request.Path.StartsWithSegments("/gdci") && !http.Request.Path.StartsWithSegments("/static/exports")) return;
            http.Response.StatusCode = StatusCodes.Status200OK;
            await http.Response.WriteAsJsonAsync(ParkingBusinessResult.Api.Contracts.ApiResponse<object>.BusinessError("Không tìm thấy chức năng hoặc tài nguyên yêu cầu."));
        });
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseRateLimiter();

        if ((app.Configuration["Authentication:Mode"] ?? "Local") == "Local")
        {
            var command = new BootstrapAdministratorCommand(app.Configuration["Authentication:BootstrapUsername"], app.Configuration["Authentication:BootstrapPassword"]);
            app.Services.GetRequiredService<BootstrapAdministratorHandler>().Handle(command);
        }
        app.MapGet("/health/live", () => Results.Ok(new { status = "healthy" })).AllowAnonymous();
        app.MapHealthChecks("/health/ready").AllowAnonymous();
        app.MapApplicationEndpoints();
    }
}
