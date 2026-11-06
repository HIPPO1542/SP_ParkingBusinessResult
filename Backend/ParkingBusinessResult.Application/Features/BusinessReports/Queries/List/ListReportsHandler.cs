using ParkingBusinessResult.Domain;
using ParkingBusinessResult.Application.Features.BusinessReports.Common;

namespace ParkingBusinessResult.Application.Features.BusinessReports.Queries.List;

public sealed class ListReportsHandler(IReportQueryRepository repository)
{
    public IReadOnlyList<BusinessResultRecord> Handle(ListReportsQuery query) => repository.List(query);
}
