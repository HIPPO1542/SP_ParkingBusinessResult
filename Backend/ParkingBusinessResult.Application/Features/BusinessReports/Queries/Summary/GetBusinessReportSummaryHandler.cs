using ParkingBusinessResult.Application.Features.BusinessReports.Queries.List;
using ParkingBusinessResult.Application.Features.BusinessReports.Common;
using ParkingBusinessResult.Domain;

namespace ParkingBusinessResult.Application.Features.BusinessReports.Queries.Summary;

public sealed class GetBusinessReportSummaryHandler(ListBusinessReportHandler listHandler)
{
    public BusinessSummary Handle(GetBusinessReportSummaryQuery query)
    {
        IReadOnlyList<ParkingBusinessResultRecord> records = listHandler.Handle(
            new ListBusinessReportQuery(query.ReportingPeriod, query.ParkingLotId, query.Access, District: query.District));
        return BusinessSummaryCalculator.Calculate(records.Select(record =>
            new BusinessSummaryEntry(record.Revenue, record.CompletedVehicleCount)));
    }
}
