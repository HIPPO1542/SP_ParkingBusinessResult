using ParkingBusinessResult.Application.Features.BusinessReports.Queries.List;
using ParkingBusinessResult.Domain;

namespace ParkingBusinessResult.Application.Features.BusinessReports.Queries.Export;

public sealed class ExportBusinessReportHandler(ListBusinessReportHandler listHandler)
{
    public IReadOnlyList<ParkingBusinessResultRecord> Handle(ExportBusinessReportQuery query) =>
        listHandler.Handle(new ListBusinessReportQuery(query.ReportingPeriod, query.ParkingLotId, query.Access));
}
