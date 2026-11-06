using ParkingBusinessResult.Application.Common;
using ParkingBusinessResult.Application.Features.ParkingBookings.Common;

namespace ParkingBusinessResult.Application.Features.ParkingBookings.Queries.Summary;

public sealed record ParkingBookingSummaryLot(Guid Id, string? Code, string? Name, string? Address);
public sealed record ParkingBookingTopLot(ParkingBookingSummaryLot ParkingLot, decimal Revenue, int VehicleCount, decimal Percentage);
public sealed record ParkingBookingDistrictRevenue(string? District, decimal Revenue, int VehicleCount, int ParkingLotsCount, decimal Percentage);
public sealed record ParkingBookingSummary(string ReportingPeriod, long CompletedVehicleCount,
    decimal CollectedRevenue, long MissingAmountCount, decimal? PreviousRevenue, decimal? GrowthRate,
    decimal? AverageDailyRevenue, IReadOnlyList<ParkingBookingTopLot>? TopRevenueParkingLots,
    IReadOnlyList<ParkingBookingDistrictRevenue>? RevenueByDistrict);

public sealed class GetParkingBookingSummaryHandler(IParkingBookingSummaryRepository repository)
{
    public ParkingBookingSummary Handle(GetParkingBookingSummaryQuery query)
    {
        CompletedParkingBooking[] permitted = repository.ListCompleted(new ParkingBookingSummaryFilter(
            query.FromInclusive, query.ToExclusive, query.Access, query.ParkingLotId, query.ParkingLotIds, query.District)).ToArray();
        decimal collected = permitted.Where(booking => booking.CollectedAmount.HasValue).Sum(booking => booking.CollectedAmount!.Value);
        long missing = permitted.LongCount(booking => booking.CollectedAmount is null);
        decimal? previousRevenue = null;
        decimal? growth = null;
        if (query.IsMonthly && query.FromInclusive > DateTime.MinValue)
        {
            CompletedParkingBooking[] previous = repository.ListCompleted(new ParkingBookingSummaryFilter(
                query.FromInclusive.AddMonths(-1), query.FromInclusive, query.Access, query.ParkingLotId, query.ParkingLotIds, query.District)).ToArray();
            if (previous.Length > 0 && previous.All(booking => booking.CollectedAmount.HasValue))
            {
                previousRevenue = previous.Sum(booking => booking.CollectedAmount!.Value);
                if (missing == 0 && previousRevenue > 0)
                    growth = Math.Round((collected - previousRevenue.Value) / previousRevenue.Value * 100, 2, MidpointRounding.AwayFromZero);
            }
        }
        int days = (query.ToExclusive - query.FromInclusive).Days;
        decimal? averageDailyRevenue = missing == 0 ? Math.Round(collected / days, 2, MidpointRounding.AwayFromZero) : null;
        ParkingBookingTopLot[]? topLots = null;
        ParkingBookingDistrictRevenue[]? districts = null;
        if (missing == 0)
        {
            topLots = permitted.GroupBy(booking => booking.ParkingLotId).Select(group =>
            {
                CompletedParkingBooking first = group.First();
                decimal revenue = group.Sum(booking => booking.CollectedAmount!.Value);
                decimal percentage = collected == 0 ? 0 : Math.Round(revenue / collected * 100, 2, MidpointRounding.AwayFromZero);
                return new ParkingBookingTopLot(new(group.Key, first.ParkingLotCode, first.ParkingLotName, first.ParkingLotAddress), revenue, group.Count(), percentage);
            }).OrderByDescending(lot => lot.Revenue).ThenBy(lot => lot.ParkingLot.Id).Take(5).ToArray();
            districts = permitted.GroupBy(booking => DistrictNames.FromAddress(booking.ParkingLotAddress) ?? "", StringComparer.OrdinalIgnoreCase)
                .Select(group =>
                {
                    decimal revenue = group.Sum(booking => booking.CollectedAmount!.Value);
                    decimal percentage = collected == 0 ? 0 : Math.Round(revenue / collected * 100, 2, MidpointRounding.AwayFromZero);
                    return new ParkingBookingDistrictRevenue(group.Key.Length == 0 ? null : group.Key, revenue, group.Count(),
                        group.Select(booking => booking.ParkingLotId).Distinct().Count(), percentage);
                }).OrderByDescending(group => group.Revenue).ThenBy(group => group.District, StringComparer.OrdinalIgnoreCase).ToArray();
        }
        return new(query.PeriodLabel,
            permitted.LongLength, collected, missing, previousRevenue, growth, averageDailyRevenue, topLots, districts);
    }

}
