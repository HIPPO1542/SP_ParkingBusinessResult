using ParkingBusinessResult.Application.Features.BusinessResults.Commands.Create;
using ParkingBusinessResult.Application.Features.BusinessReports.Queries.List;
using ParkingBusinessResult.Application.Features.BusinessReports.Queries.Export;
using ParkingBusinessResult.Application.Features.BusinessReports.Queries.Summary;
using ParkingBusinessResult.Application.Features.ParkingVisits.Commands.Import;
using ParkingBusinessResult.Application.Features.ParkingVisits.Queries.Summary;
using ParkingBusinessResult.Application.Features.ParkingVisits.Queries.Export;
using ParkingBusinessResult.Application.Features.ParkingVisits.Queries.CompareTariff;
using ParkingBusinessResult.Application.Features.ParkingVisits.Queries.List;
using ParkingBusinessResult.Application.Features.ParkingVisits.Queries.Filters;
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
        services.AddScoped<ImportParkingVisitsHandler>();
        services.AddScoped<GetParkingVisitSummaryHandler>();
        services.AddScoped<ExportParkingVisitReportHandler>();
        services.AddScoped<CompareParkingVisitTariffHandler>();
        services.AddScoped<ListParkingVisitsHandler>();
        services.AddScoped<GetParkingVisitFiltersHandler>();
        services.AddSingleton<BootstrapAdministratorHandler>();
        services.AddSingleton<CreateAccountHandler>();
        services.AddSingleton<SignInHandler>();
        return services;
    }
}
