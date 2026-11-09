using ParkingBusinessResult.Application.Features.BusinessReports.Common;
using ParkingBusinessResult.Application.Features.BusinessResults.Common;
using ParkingBusinessResult.Application.Features.ParkingBookings.Common;

namespace ParkingBusinessResult.Application.Features.ParkingBookings.Queries.Filters;

public sealed record GetBookingFiltersQuery(BusinessAccess Access, int? Day = null, int? Month = null, int? Year = null);
public sealed record OverviewVehicleCount(string VehicleType, long VehicleCount, decimal Percentage);
public sealed record OverviewLotRevenue(Guid ParkingLotId, decimal? Revenue, decimal? Percentage);
public sealed record OverviewStatusCount(string Status, long BookingCount, decimal Percentage);
public sealed record BookingOverview(string ReportingPeriod, long TotalVehicleCount, decimal? TotalRevenue, long MissingAmountCount,
    IReadOnlyList<OverviewVehicleCount> VehicleCounts, IReadOnlyList<OverviewLotRevenue> RevenueByParkingLot,
    IReadOnlyList<OverviewStatusCount> StatusCounts);
public sealed record BookingFilters(IReadOnlyList<ReportParkingLot> ParkingLots, IReadOnlyList<string> VehicleTypes, BookingOverview Overview);

public sealed class GetBookingFiltersHandler(IBookingListRepository bookings, IReportParkingLotSearch parkingLots)
{
    public BookingFilters Handle(GetBookingFiltersQuery query)
    {
        HashSet<Guid> permittedIds = parkingLots.FindMatchingLots(null, null).Where(query.Access.CanAccess).ToHashSet();
        ReportParkingLot[] lots = parkingLots.GetParkingLots(permittedIds).Values
            .OrderBy(lot => lot.Code, StringComparer.OrdinalIgnoreCase).ThenBy(lot => lot.Name, StringComparer.OrdinalIgnoreCase).ToArray();
        IReadOnlyList<string> vehicleTypes = bookings.GetVehicleTypes(query.Access);
        BookingOverviewGroup[] groups = bookings.GetOverview(query.Access, query.Day, query.Month, query.Year)
            .Where(group => permittedIds.Contains(group.ParkingLotId)).ToArray();
        vehicleTypes = vehicleTypes.Concat(groups.Select(group => group.VehicleType)).Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(type => type, StringComparer.OrdinalIgnoreCase).ToArray();
        long totalVehicles = groups.Sum(group => group.CompletedCount + group.ParkedCount);
        long missingAmounts = groups.Sum(group => group.MissingAmountCount);
        decimal? totalRevenue = missingAmounts == 0 ? groups.Sum(group => group.Revenue) : null;
        var byVehicle = groups.ToLookup(group => group.VehicleType, StringComparer.OrdinalIgnoreCase);
        OverviewVehicleCount[] counts = vehicleTypes.Select(type =>
        {
            long count = byVehicle[type].Sum(group => group.CompletedCount + group.ParkedCount);
            return new OverviewVehicleCount(type, count, Percentage(count, totalVehicles));
        }).ToArray();
        var byLot = groups.ToLookup(group => group.ParkingLotId);
        OverviewLotRevenue[] revenues = lots.Select(lot =>
        {
            decimal? revenue = byLot[lot.Id].Any(group => group.MissingAmountCount > 0) ? null : byLot[lot.Id].Sum(group => group.Revenue);
            decimal? percentage = revenue.HasValue && totalRevenue.HasValue ? Percentage(revenue.Value, totalRevenue.Value) : null;
            return new OverviewLotRevenue(lot.Id, revenue, percentage);
        }).ToArray();
        long completed = groups.Sum(group => group.CompletedCount);
        long parked = groups.Sum(group => group.ParkedCount);
        long reserved = groups.Sum(group => group.ReservedCount);
        long totalBookings = completed + parked + reserved;
        OverviewStatusCount[] statuses =
        [
            new("COMPLETED", completed, Percentage(completed, totalBookings)),
            new("CHECKED_IN", parked, Percentage(parked, totalBookings)),
            new("RESERVED", reserved, Percentage(reserved, totalBookings))
        ];
        string period = query.Year is null ? "Toàn bộ thời gian" : query.Day.HasValue ? $"{query.Day:D2}/{query.Month:D2}/{query.Year:D4}" :
            query.Month.HasValue ? $"{query.Month:D2}/{query.Year:D4}" : $"Năm {query.Year:D4}";
        return new(lots, vehicleTypes, new(period, totalVehicles, totalRevenue, missingAmounts, counts, revenues, statuses));
    }

    private static decimal Percentage(decimal value, decimal total) => total == 0 ? 0 :
        Math.Round(value / total * 100, 2, MidpointRounding.AwayFromZero);
}
