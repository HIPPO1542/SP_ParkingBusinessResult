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
using ParkingBusinessResult.Application.Features.LocalAccounts.Commands.Register;
using ParkingBusinessResult.Application.Features.ParkingBookings.Commands.Reserve;
using ParkingBusinessResult.Application.Features.ParkingBookings.Commands.CheckIn;
using ParkingBusinessResult.Application.Features.ParkingBookings.Commands.CheckOut;
using ParkingBusinessResult.Application.Features.ParkingBookings.Queries.Options;
using ParkingBusinessResult.Application.Features.ParkingBookings.Queries.MyBookings;

namespace ParkingBusinessResult.Api.Extensions;

public static class BusinessResultRegistration
{
    public static IServiceCollection AddBusinessResultHandlers(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<ReserveBookingHandler>();
        services.AddScoped<CheckInHandler>();
        services.AddScoped<CheckOutHandler>();
        services.AddScoped<GetBookingOptionsHandler>();
        services.AddScoped<GetMyBookingsHandler>();
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
        services.AddSingleton<RegisterCustomerHandler>();
        return services;
    }
}
