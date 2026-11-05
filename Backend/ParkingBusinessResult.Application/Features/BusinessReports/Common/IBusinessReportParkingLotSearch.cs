namespace ParkingBusinessResult.Application.Features.BusinessReports.Common;

public interface IBusinessReportParkingLotSearch
{
    IReadOnlySet<Guid> FindMatchingLots(string? keyword, string? district);
    IReadOnlyDictionary<Guid, ReportParkingLot> GetParkingLots(IReadOnlySet<Guid> ids);
}
