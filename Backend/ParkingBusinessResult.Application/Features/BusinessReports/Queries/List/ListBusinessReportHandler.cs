using ParkingBusinessResult.Domain;
using ParkingBusinessResult.Application.Features.BusinessReports.Common;

namespace ParkingBusinessResult.Application.Features.BusinessReports.Queries.List;

public sealed class ListBusinessReportHandler(IBusinessResultRepository repository, IBusinessReportParkingLotSearch parkingLotSearch)
{
    public IReadOnlyList<ParkingBusinessResultRecord> Handle(ListBusinessReportQuery query)
    {
        IReadOnlySet<Guid>? matchingLots = string.IsNullOrWhiteSpace(query.Keyword)
            ? null : parkingLotSearch.FindMatchingLots(query.Keyword.Trim());
        return repository.List()
            .Where(record => query.Access.CanAccess(record.ParkingLotId))
            .Where(record => record.ReportingPeriod == query.ReportingPeriod)
            .Where(record => query.ParkingLotId is null || record.ParkingLotId == query.ParkingLotId)
            .Where(record => matchingLots is null || matchingLots.Contains(record.ParkingLotId))
            .OrderBy(record => record.ParkingLotId)
            .ToArray();
    }
}
