using ParkingBusinessResult.Application.Features.ParkingVisits.Common;

namespace ParkingBusinessResult.Application.Features.ParkingVisits.Queries.Summary;

public sealed record ParkingVisitSummaryLot(Guid Id, string? Code, string? Name, string? Address);
public sealed record ParkingVisitTopLot(ParkingVisitSummaryLot ParkingLot, decimal Revenue, int VehicleCount, decimal Percentage);
public sealed record ParkingVisitDistrictRevenue(string? District, decimal Revenue, int VehicleCount, int ParkingLotsCount, decimal Percentage);
public sealed record ParkingVisitSummary(string ReportingPeriod, long CompletedVehicleCount,
    decimal CollectedRevenue, long MissingAmountCount, decimal? PreviousRevenue, decimal? GrowthRate,
    decimal? AverageDailyRevenue, IReadOnlyList<ParkingVisitTopLot>? TopRevenueParkingLots,
    IReadOnlyList<ParkingVisitDistrictRevenue>? RevenueByDistrict);

public sealed class GetParkingVisitSummaryHandler(IParkingVisitSummaryRepository repository)
{
    public ParkingVisitSummary Handle(GetParkingVisitSummaryQuery query)
    {
        IReadOnlyList<CompletedParkingVisit> visits = repository.ListCompleted(query.FromInclusive, query.ToExclusive);
        CompletedParkingVisit[] permitted = Filter(visits, query);
        decimal collected = permitted.Where(visit => visit.CollectedAmount.HasValue).Sum(visit => visit.CollectedAmount!.Value);
        long missing = permitted.LongCount(visit => visit.CollectedAmount is null);
        decimal? previousRevenue = null;
        decimal? growth = null;
        if (query.IsMonthly && query.FromInclusive > DateTime.MinValue)
        {
            CompletedParkingVisit[] previous = Filter(repository.ListCompleted(query.FromInclusive.AddMonths(-1), query.FromInclusive), query);
            if (previous.Length > 0 && previous.All(visit => visit.CollectedAmount.HasValue))
            {
                previousRevenue = previous.Sum(visit => visit.CollectedAmount!.Value);
                if (missing == 0 && previousRevenue > 0)
                    growth = Math.Round((collected - previousRevenue.Value) / previousRevenue.Value * 100, 2, MidpointRounding.AwayFromZero);
            }
        }
        int days = (query.ToExclusive - query.FromInclusive).Days;
        decimal? averageDailyRevenue = missing == 0 ? Math.Round(collected / days, 2, MidpointRounding.AwayFromZero) : null;
        ParkingVisitTopLot[]? topLots = null;
        ParkingVisitDistrictRevenue[]? districts = null;
        if (missing == 0)
        {
            topLots = permitted.GroupBy(visit => visit.ParkingLotId).Select(group =>
            {
                CompletedParkingVisit first = group.First();
                decimal revenue = group.Sum(visit => visit.CollectedAmount!.Value);
                decimal percentage = collected == 0 ? 0 : Math.Round(revenue / collected * 100, 2, MidpointRounding.AwayFromZero);
                return new ParkingVisitTopLot(new(group.Key, first.ParkingLotCode, first.ParkingLotName, first.ParkingLotAddress), revenue, group.Count(), percentage);
            }).OrderByDescending(lot => lot.Revenue).ThenBy(lot => lot.ParkingLot.Id).Take(5).ToArray();
            districts = permitted.GroupBy(visit => FindDistrict(visit.ParkingLotAddress) ?? "", StringComparer.OrdinalIgnoreCase)
                .Select(group =>
                {
                    decimal revenue = group.Sum(visit => visit.CollectedAmount!.Value);
                    decimal percentage = collected == 0 ? 0 : Math.Round(revenue / collected * 100, 2, MidpointRounding.AwayFromZero);
                    return new ParkingVisitDistrictRevenue(group.Key.Length == 0 ? null : group.Key, revenue, group.Count(),
                        group.Select(visit => visit.ParkingLotId).Distinct().Count(), percentage);
                }).OrderByDescending(group => group.Revenue).ThenBy(group => group.District, StringComparer.OrdinalIgnoreCase).ToArray();
        }
        return new(query.PeriodLabel,
            permitted.LongLength, collected, missing, previousRevenue, growth, averageDailyRevenue, topLots, districts);
    }

    private static CompletedParkingVisit[] Filter(IEnumerable<CompletedParkingVisit> visits, GetParkingVisitSummaryQuery query) =>
        visits.Where(visit => query.Access.CanAccess(visit.ParkingLotId))
            .Where(visit => query.ParkingLotId is null || visit.ParkingLotId == query.ParkingLotId)
            .Where(visit => query.ParkingLotIds is null || query.ParkingLotIds.Contains(visit.ParkingLotId))
            .Where(visit => string.IsNullOrWhiteSpace(query.District) || (visit.ParkingLotAddress?.Contains(query.District.Trim(), StringComparison.OrdinalIgnoreCase) ?? false)).ToArray();

    private static string? FindDistrict(string? address)
    {
        if (address is null) return null;
        string[] matches = address.Split(',').Select(part => part.Trim())
            .Where(part => part.StartsWith("Quận ", StringComparison.OrdinalIgnoreCase) || part.StartsWith("Huyện ", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        return matches.Length == 1 ? matches[0] : null;
    }
}
