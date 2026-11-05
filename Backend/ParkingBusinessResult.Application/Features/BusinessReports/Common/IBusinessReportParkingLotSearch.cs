namespace ParkingBusinessResult.Application.Features.BusinessReports.Common;

public interface IBusinessReportParkingLotSearch
{
    IReadOnlySet<Guid> FindMatchingLots(string keyword);
}
