using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ParkingBusinessResult.Domain;
using ParkingBusinessResult.Application.Features.BusinessReports.Common;
using ParkingBusinessResult.Application.Features.BusinessReports.Queries.List;
using ParkingBusinessResult.Application.Features.ParkingBookings.Common;
using ParkingBusinessResult.Application.Features.LocalAccounts.Common;
using ParkingBusinessResult.Infrastructure.Repositories;

namespace ParkingBusinessResult.Infrastructure.Persistence;

public static class PersistenceRegistration
{
    public static IServiceCollection AddParkingPersistence(this IServiceCollection services, string databasePath)
    {
        services.AddDbContext<ParkingDbContext>(options => ParkingContextFactory.ConfigureOptions(options, databasePath));
        services.AddScoped<IBusinessResultRepository, BusinessResultRepository>();
        services.AddScoped<IReportQueryRepository, ReportQueryRepository>();
        services.AddScoped<IReportParkingLotSearch, ReportParkingLotSearch>();
        services.AddScoped<IReportMetadataReader, ReportMetadataReader>();
        services.AddScoped<IBookingImportRepository, BookingImportRepository>();
        services.AddScoped<BookingQueryRepository>();
        services.AddScoped<IBookingListRepository>(provider => provider.GetRequiredService<BookingQueryRepository>());
        services.AddScoped<IBookingSummaryRepository>(provider => provider.GetRequiredService<BookingQueryRepository>());
        services.AddScoped<IBookingTariffRepository, BookingTariffRepository>();
        services.AddScoped<IBookingLifecycleRepository, BookingLifecycleRepository>();
        services.AddScoped<ICustomerBookingQueries, BookingLifecycleRepository>();
        services.AddScoped<ILocalAccountRepository, LocalAccountRepository>();
        return services;
    }
}
