using ParkingBusinessResult.Domain;
using ParkingBusinessResult.Application.Features.BusinessReports.Queries.List;

namespace ParkingBusinessResult.Application.Features.BusinessReports.Common;

public interface IBusinessReportQueryRepository
{
    IReadOnlyList<ParkingBusinessResultRecord> List(ListBusinessReportQuery query);
}
