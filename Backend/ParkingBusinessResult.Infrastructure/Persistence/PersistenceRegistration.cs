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
        services.AddSingleton<IBusinessResultRepository, SqliteBusinessResultRepository>();
        services.AddSingleton<IBusinessReportQueryRepository, SqliteBusinessReportQueryRepository>();
        services.AddSingleton<IBusinessReportParkingLotSearch, SqliteBusinessReportParkingLotSearch>();
        services.AddSingleton<IBusinessReportMetadataReader, SqliteBusinessReportMetadataReader>();
        services.AddSingleton<IParkingBookingImportRepository, SqliteParkingBookingImportRepository>();
        services.AddSingleton<SqliteParkingBookingQueryRepository>();
        services.AddSingleton<IParkingBookingListRepository>(provider => provider.GetRequiredService<SqliteParkingBookingQueryRepository>());
        services.AddSingleton<IParkingBookingSummaryRepository>(provider => provider.GetRequiredService<SqliteParkingBookingQueryRepository>());
        services.AddSingleton<IParkingBookingTariffRepository, SqliteParkingBookingTariffRepository>();
        services.AddSingleton<ILocalAccountRepository, SqliteLocalAccountRepository>();
        return services;
    }
}
