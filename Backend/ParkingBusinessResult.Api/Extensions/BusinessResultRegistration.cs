using ParkingBusinessResult.Application.Features.BusinessResults.Commands.Create;
using ParkingBusinessResult.Application.Features.BusinessReports.Queries.List;
using ParkingBusinessResult.Application.Features.BusinessReports.Commands.Export;
using ParkingBusinessResult.Application.Features.BusinessReports.Queries.Download;
using ParkingBusinessResult.Application.Features.BusinessReports.Queries.Summary;
using ParkingBusinessResult.Application.Features.ParkingBookings.Commands.Import;
using ParkingBusinessResult.Application.Features.ParkingBookings.Queries.Summary;
using ParkingBusinessResult.Application.Features.ParkingBookings.Commands.Export;
using ParkingBusinessResult.Application.Features.ParkingBookings.Queries.CompareTariff;
using ParkingBusinessResult.Application.Features.ParkingBookings.Queries.List;
using ParkingBusinessResult.Application.Features.ParkingBookings.Queries.Filters;
using ParkingBusinessResult.Application.Features.BusinessResults.Commands.Delete;
using ParkingBusinessResult.Application.Features.BusinessResults.Commands.Update;
using ParkingBusinessResult.Application.Features.BusinessResults.Queries.GetById;
using ParkingBusinessResult.Application.Features.BusinessResults.Queries.List;
using ParkingBusinessResult.Application.Features.LocalAccounts.Commands.Bootstrap;
using ParkingBusinessResult.Application.Features.LocalAccounts.Commands.Create;
using ParkingBusinessResult.Application.Features.LocalAccounts.Commands.SignIn;

namespace ParkingBusinessResult.Api.Extensions;

public static class BusinessResultRegistration
{
    public static IServiceCollection AddBusinessResultHandlers(this IServiceCollection services)
    {
        services.AddScoped<CreateBusinessResultHandler>();
        services.AddScoped<UpdateBusinessResultHandler>();
        services.AddScoped<DeleteBusinessResultHandler>();
        services.AddScoped<GetBusinessResultHandler>();
        services.AddScoped<ListBusinessResultsHandler>();
        services.AddScoped<ListReportsHandler>();
        services.AddScoped<ReportListMapper>();
        services.AddScoped<GetReportSummaryHandler>();
        services.AddScoped<ExportReportHandler>();
        services.AddScoped<DownloadReportHandler>();
        services.AddScoped<ImportBookingsHandler>();
        services.AddScoped<GetBookingSummaryHandler>();
        services.AddScoped<ExportBookingReportHandler>();
        services.AddScoped<CompareBookingTariffHandler>();
        services.AddScoped<ListBookingsHandler>();
        services.AddScoped<GetBookingFiltersHandler>();
        services.AddSingleton<BootstrapAdministratorHandler>();
        services.AddSingleton<CreateAccountHandler>();
        services.AddSingleton<SignInHandler>();
        return services;
    }
}
