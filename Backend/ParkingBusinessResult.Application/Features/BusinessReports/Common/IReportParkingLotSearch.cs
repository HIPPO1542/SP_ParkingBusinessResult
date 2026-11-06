namespace ParkingBusinessResult.Application.Features.BusinessReports.Common;

public interface IReportParkingLotSearch
{
    IReadOnlySet<Guid> FindMatchingLots(string? keyword, string? district);
    IReadOnlyDictionary<Guid, ReportParkingLot> GetParkingLots(IReadOnlySet<Guid> ids);
}
