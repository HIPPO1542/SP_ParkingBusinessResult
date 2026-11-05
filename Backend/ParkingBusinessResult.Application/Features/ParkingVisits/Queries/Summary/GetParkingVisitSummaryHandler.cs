using ParkingBusinessResult.Application.Features.ParkingVisits.Common;

namespace ParkingBusinessResult.Application.Features.ParkingVisits.Queries.Summary;

public sealed record ParkingVisitSummary(string ReportingPeriod, long CompletedVehicleCount,
    decimal CollectedRevenue, long MissingAmountCount, decimal? PreviousRevenue, decimal? GrowthRate,
    decimal? AverageDailyRevenue);

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
        return new(query.PeriodLabel,
            permitted.LongLength, collected, missing, previousRevenue, growth, averageDailyRevenue);
    }

    private static CompletedParkingVisit[] Filter(IEnumerable<CompletedParkingVisit> visits, GetParkingVisitSummaryQuery query) =>
        visits.Where(visit => query.Access.CanAccess(visit.ParkingLotId))
            .Where(visit => query.ParkingLotId is null || visit.ParkingLotId == query.ParkingLotId).ToArray();
}
