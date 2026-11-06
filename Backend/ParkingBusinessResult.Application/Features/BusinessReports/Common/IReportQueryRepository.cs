using ParkingBusinessResult.Domain;
using ParkingBusinessResult.Application.Features.BusinessReports.Queries.List;

namespace ParkingBusinessResult.Application.Features.BusinessReports.Common;

public interface IReportQueryRepository
{
    IReadOnlyList<BusinessResultRecord> List(ListReportsQuery query);
}
