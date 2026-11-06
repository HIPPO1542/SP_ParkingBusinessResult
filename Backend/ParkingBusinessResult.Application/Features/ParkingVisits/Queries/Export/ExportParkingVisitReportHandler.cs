using ParkingBusinessResult.Application.Features.BusinessReports.Common;
using ParkingBusinessResult.Application.Features.ParkingVisits.Common;

namespace ParkingBusinessResult.Application.Features.ParkingVisits.Queries.Export;

public sealed record ParkingVisitExportResult(IReadOnlyList<BusinessReportRow> Rows, int MissingAmountCount);

public sealed class ExportParkingVisitReportHandler(IParkingVisitSummaryRepository repository)
{
    public ParkingVisitExportResult Handle(ExportParkingVisitReportQuery query)
    {
        CompletedParkingVisit[] visits = repository.ListCompleted(query.FromInclusive, query.ToExclusive)
            .Where(visit => query.Access.CanAccess(visit.ParkingLotId))
            .Where(visit => query.ParkingLotId is null || visit.ParkingLotId == query.ParkingLotId)
            .Where(visit => query.ParkingLotIds is null || query.ParkingLotIds.Contains(visit.ParkingLotId))
            .Where(visit => string.IsNullOrWhiteSpace(query.District) || (visit.ParkingLotAddress?.Contains(query.District.Trim(), StringComparison.OrdinalIgnoreCase) ?? false)).ToArray();
        int missing = visits.Count(visit => visit.CollectedAmount is null);
        BusinessReportRow[] rows = visits.GroupBy(visit => visit.ParkingLotId).OrderBy(group => group.Key)
            .Select(group => new BusinessReportRow(group.Key, query.PeriodLabel,
                group.Where(visit => visit.CollectedAmount.HasValue).Sum(visit => visit.CollectedAmount!.Value), group.Count(),
                group.First().ParkingLotCode, group.First().ParkingLotName)).ToArray();
        return new(rows, missing);
    }
}
