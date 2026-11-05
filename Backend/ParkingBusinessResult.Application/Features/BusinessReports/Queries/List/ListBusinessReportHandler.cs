using ParkingBusinessResult.Domain;
using ParkingBusinessResult.Application.Features.BusinessReports.Common;

namespace ParkingBusinessResult.Application.Features.BusinessReports.Queries.List;

public sealed class ListBusinessReportHandler(IBusinessResultRepository repository, IBusinessReportParkingLotSearch parkingLotSearch)
{
    public IReadOnlyList<ParkingBusinessResultRecord> Handle(ListBusinessReportQuery query)
    {
        string? keyword = string.IsNullOrWhiteSpace(query.Keyword) ? null : query.Keyword.Trim();
        string? district = string.IsNullOrWhiteSpace(query.District) ? null : query.District.Trim();
        IReadOnlySet<Guid>? matchingLots = keyword is null && district is null
            ? null : parkingLotSearch.FindMatchingLots(keyword, district);
        return repository.List()
            .Where(record => query.Access.CanAccess(record.ParkingLotId))
            .Where(record => query.ReportingPeriod is null || record.ReportingPeriod == query.ReportingPeriod)
            .Where(record => query.ParkingLotId is null || record.ParkingLotId == query.ParkingLotId)
            .Where(record => matchingLots is null || matchingLots.Contains(record.ParkingLotId))
            .OrderByDescending(record => record.Revenue).ThenBy(record => record.ParkingLotId)
            .ToArray();
    }
}
