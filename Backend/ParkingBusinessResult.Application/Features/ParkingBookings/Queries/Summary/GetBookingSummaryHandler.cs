using ParkingBusinessResult.Application.Common;
using ParkingBusinessResult.Application.Features.ParkingBookings.Common;

namespace ParkingBusinessResult.Application.Features.ParkingBookings.Queries.Summary;

public sealed record BookingSummaryLot(Guid Id, string? Code, string? Name, string? Address);
public sealed record BookingVehicleRevenue(string VehicleType, decimal Revenue, int VehicleCount, decimal? Percentage);
public sealed record BookingTopLot(BookingSummaryLot ParkingLot, decimal Revenue, int VehicleCount, decimal Percentage,
    IReadOnlyList<BookingVehicleRevenue> RevenueByVehicleType);
public sealed record BookingDistrictRevenue(string? District, decimal Revenue, int VehicleCount, int ParkingLotsCount, decimal Percentage);
public sealed record BookingSummary(string ReportingPeriod, long CompletedVehicleCount,
    decimal CollectedRevenue, long MissingAmountCount, decimal? PreviousRevenue, decimal? GrowthRate,
    decimal? AverageDailyRevenue, IReadOnlyList<BookingTopLot>? TopRevenueParkingLots,
    IReadOnlyList<BookingDistrictRevenue>? RevenueByDistrict, IReadOnlyList<BookingVehicleRevenue>? RevenueByVehicleType);

public sealed class GetBookingSummaryHandler(IBookingSummaryRepository repository)
{
    public BookingSummary Handle(GetBookingSummaryQuery query)
    {
        IReadOnlyList<CompletedBooking> allVehicleTypes = repository.ListCompleted(new BookingSummaryFilter(
            query.FromInclusive, query.ToExclusive, query.Access, query.ParkingLotId, query.ParkingLotIds, query.District));
        CompletedBooking[] permitted = allVehicleTypes.Where(booking => query.VehicleType is null ||
            string.Equals(booking.VehicleType, query.VehicleType, StringComparison.OrdinalIgnoreCase)).ToArray();
        decimal collected = permitted.Where(booking => booking.CollectedAmount.HasValue).Sum(booking => booking.CollectedAmount!.Value);
        long missing = permitted.LongCount(booking => booking.CollectedAmount is null);
        decimal? previousRevenue = null;
        decimal? growth = null;
        if (query.IsMonthly && query.FromInclusive > DateTime.MinValue)
        {
            CompletedBooking[] previous = repository.ListCompleted(new BookingSummaryFilter(
                query.FromInclusive.AddMonths(-1), query.FromInclusive, query.Access, query.ParkingLotId, query.ParkingLotIds, query.District, query.VehicleType)).ToArray();
            if (previous.Length > 0 && previous.All(booking => booking.CollectedAmount.HasValue))
            {
                previousRevenue = previous.Sum(booking => booking.CollectedAmount!.Value);
                if (missing == 0 && previousRevenue > 0)
                    growth = Math.Round((collected - previousRevenue.Value) / previousRevenue.Value * 100, 2, MidpointRounding.AwayFromZero);
            }
        }
        int days = (query.ToExclusive - query.FromInclusive).Days;
        decimal? averageDailyRevenue = missing == 0 ? Math.Round(collected / days, 2, MidpointRounding.AwayFromZero) : null;
        BookingTopLot[]? topLots = null;
        BookingDistrictRevenue[]? districts = null;
        BookingVehicleRevenue[]? vehicles = null;
        if (missing == 0)
        {
            // Keep all vehicle types in the denominator, even when the displayed totals are filtered.
            var lotTotals = allVehicleTypes.GroupBy(booking => booking.ParkingLotId).ToDictionary(group => group.Key,
                group => group.Any(booking => booking.CollectedAmount is null) ? (decimal?)null : group.Sum(booking => booking.CollectedAmount!.Value));
            decimal? overallTotal = lotTotals.Values.Any(value => value is null) ? null : lotTotals.Values.Sum(value => value!.Value);
            vehicles = GroupByVehicleType(permitted, overallTotal);
            topLots = permitted.GroupBy(booking => booking.ParkingLotId).Select(group =>
            {
                CompletedBooking first = group.First();
                decimal revenue = group.Sum(booking => booking.CollectedAmount!.Value);
                decimal percentage = collected == 0 ? 0 : Math.Round(revenue / collected * 100, 2, MidpointRounding.AwayFromZero);
                return new BookingTopLot(new(group.Key, first.ParkingLotCode, first.ParkingLotName, first.ParkingLotAddress),
                    revenue, group.Count(), percentage, GroupByVehicleType(group, lotTotals[group.Key]));
            }).OrderByDescending(lot => lot.Revenue).ThenBy(lot => lot.ParkingLot.Id).Take(5).ToArray();
            districts = permitted.GroupBy(booking => DistrictNames.FromAddress(booking.ParkingLotAddress) ?? "", StringComparer.OrdinalIgnoreCase)
                .Select(group =>
                {
                    decimal revenue = group.Sum(booking => booking.CollectedAmount!.Value);
                    decimal percentage = collected == 0 ? 0 : Math.Round(revenue / collected * 100, 2, MidpointRounding.AwayFromZero);
                    return new BookingDistrictRevenue(group.Key.Length == 0 ? null : group.Key, revenue, group.Count(),
                        group.Select(booking => booking.ParkingLotId).Distinct().Count(), percentage);
                }).OrderByDescending(group => group.Revenue).ThenBy(group => group.District, StringComparer.OrdinalIgnoreCase).ToArray();
        }
        return new(query.PeriodLabel,
            permitted.LongLength, collected, missing, previousRevenue, growth, averageDailyRevenue, topLots, districts, vehicles);
    }

    private static BookingVehicleRevenue[] GroupByVehicleType(IEnumerable<CompletedBooking> bookings, decimal? totalRevenue) =>
        bookings.GroupBy(booking => booking.VehicleType, StringComparer.OrdinalIgnoreCase).Select(group =>
        {
            decimal revenue = group.Sum(booking => booking.CollectedAmount!.Value);
            decimal? percentage = totalRevenue is null ? null : totalRevenue == 0 ? 0 :
                Math.Round(revenue / totalRevenue.Value * 100, 2, MidpointRounding.AwayFromZero);
            return new BookingVehicleRevenue(group.Key, revenue, group.Count(), percentage);
        }).OrderByDescending(item => item.Revenue).ThenBy(item => item.VehicleType, StringComparer.OrdinalIgnoreCase).ToArray();
}
