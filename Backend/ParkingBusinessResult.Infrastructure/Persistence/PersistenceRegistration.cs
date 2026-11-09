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
        services.AddSingleton<IDbContextFactory<ParkingDbContext>>(new ParkingContextFactory(databasePath));
        services.AddSingleton<IBusinessResultRepository, BusinessResultRepository>();
        services.AddSingleton<IReportQueryRepository, ReportQueryRepository>();
        services.AddSingleton<IReportParkingLotSearch, ReportParkingLotSearch>();
        services.AddSingleton<IReportMetadataReader, ReportMetadataReader>();
        services.AddSingleton<IBookingImportRepository, BookingImportRepository>();
        services.AddSingleton<BookingQueryRepository>();
        services.AddSingleton<IBookingListRepository>(provider => provider.GetRequiredService<BookingQueryRepository>());
        services.AddSingleton<IBookingSummaryRepository>(provider => provider.GetRequiredService<BookingQueryRepository>());
        services.AddSingleton<IBookingTariffRepository, BookingTariffRepository>();
        services.AddSingleton<IBookingLifecycleRepository, BookingLifecycleRepository>();
        services.AddSingleton<ICustomerBookingQueries, BookingLifecycleRepository>();
        services.AddSingleton<ILocalAccountRepository, LocalAccountRepository>();
        return services;
    }
}
