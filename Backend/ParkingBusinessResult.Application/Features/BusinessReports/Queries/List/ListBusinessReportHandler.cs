using ParkingBusinessResult.Domain;

namespace ParkingBusinessResult.Application.Features.BusinessReports.Queries.List;

public sealed class ListBusinessReportHandler(IBusinessResultRepository repository)
{
    public IReadOnlyList<ParkingBusinessResultRecord> Handle(ListBusinessReportQuery query) =>
        repository.List()
            .Where(record => query.Access.CanAccess(record.ParkingLotId))
            .Where(record => record.ReportingPeriod == query.ReportingPeriod)
            .Where(record => query.ParkingLotId is null || record.ParkingLotId == query.ParkingLotId)
            .OrderBy(record => record.ParkingLotId)
            .ToArray();
}
