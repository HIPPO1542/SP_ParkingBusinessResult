using ParkingBusinessResult.Application.Features.BusinessResults.Commands.Create;
using ParkingBusinessResult.Application.Features.BusinessReports.Queries.List;
using ParkingBusinessResult.Application.Features.BusinessReports.Queries.Export;
using ParkingBusinessResult.Application.Features.BusinessReports.Queries.Summary;
using ParkingBusinessResult.Application.Features.ParkingBookings.Commands.Import;
using ParkingBusinessResult.Application.Features.ParkingBookings.Queries.Summary;
using ParkingBusinessResult.Application.Features.ParkingBookings.Queries.Export;
using ParkingBusinessResult.Application.Features.ParkingBookings.Queries.CompareTariff;
using ParkingBusinessResult.Application.Features.ParkingBookings.Queries.List;
using ParkingBusinessResult.Application.Features.ParkingBookings.Queries.Filters;
using ParkingBusinessResult.Application.Features.BusinessResults.Commands.Delete;
using ParkingBusinessResult.Application.Features.BusinessResults.Commands.Update;
using ParkingBusinessResult.Application.Features.BusinessResults.Queries.GetById;
using ParkingBusinessResult.Application.Features.BusinessResults.Queries.List;
using ParkingBusinessResult.Application.Features.DevelopmentAccounts.Commands.Bootstrap;
using ParkingBusinessResult.Application.Features.DevelopmentAccounts.Commands.Create;
using ParkingBusinessResult.Application.Features.DevelopmentAccounts.Commands.SignIn;

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
        services.AddScoped<ListBusinessReportHandler>();
        services.AddScoped<BusinessReportListMapper>();
        services.AddScoped<GetBusinessReportSummaryHandler>();
        services.AddScoped<ExportBusinessReportHandler>();
        services.AddScoped<ImportParkingBookingsHandler>();
        services.AddScoped<GetParkingBookingSummaryHandler>();
        services.AddScoped<ExportParkingBookingReportHandler>();
        services.AddScoped<CompareParkingBookingTariffHandler>();
        services.AddScoped<ListParkingBookingsHandler>();
        services.AddScoped<GetParkingBookingFiltersHandler>();
        services.AddSingleton<BootstrapAdministratorHandler>();
        services.AddSingleton<CreateAccountHandler>();
        services.AddSingleton<SignInHandler>();
        return services;
    }
}
