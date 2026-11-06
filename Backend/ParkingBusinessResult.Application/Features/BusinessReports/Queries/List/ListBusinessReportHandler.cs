using ParkingBusinessResult.Domain;
using ParkingBusinessResult.Application.Features.BusinessReports.Common;

namespace ParkingBusinessResult.Application.Features.BusinessReports.Queries.List;

public sealed class ListBusinessReportHandler(IBusinessReportQueryRepository repository)
{
    public IReadOnlyList<ParkingBusinessResultRecord> Handle(ListBusinessReportQuery query) => repository.List(query);
}
