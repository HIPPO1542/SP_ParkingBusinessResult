namespace ParkingBusinessResult.Application.Features.ParkingVisits.Common;

public sealed record CompletedParkingVisit(Guid ParkingLotId, decimal? CollectedAmount);

public interface IParkingVisitSummaryRepository
{
    IReadOnlyList<CompletedParkingVisit> ListCompleted(DateTime fromInclusive, DateTime toExclusive);
}
