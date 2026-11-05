using ParkingBusinessResult.Application.Features.BusinessReports.Queries.List;
using ParkingBusinessResult.Application.Features.BusinessReports.Common;
using ParkingBusinessResult.Domain;

namespace ParkingBusinessResult.Application.Features.BusinessReports.Queries.Summary;

public sealed class GetBusinessReportSummaryHandler(ListBusinessReportHandler listHandler, IBusinessReportParkingLotSearch parkingLotSearch)
{
    public BusinessReportSummary Handle(GetBusinessReportSummaryQuery query)
    {
        IReadOnlyList<ParkingBusinessResultRecord> records = listHandler.Handle(
            new ListBusinessReportQuery(query.ReportingPeriod, query.ParkingLotId, query.Access, District: query.District));
        BusinessSummary totals = BusinessSummaryCalculator.Calculate(records.Select(record =>
            new BusinessSummaryEntry(record.Revenue, record.CompletedVehicleCount)));
        ParkingBusinessResultRecord[] topRecords = records.OrderByDescending(record => record.Revenue)
            .ThenBy(record => record.ParkingLotId).Take(5).ToArray();
        IReadOnlyDictionary<Guid, ReportParkingLot> lots = parkingLotSearch.GetParkingLots(
            topRecords.Select(record => record.ParkingLotId).ToHashSet());
        TopRevenueParkingLot[] topLots = topRecords.Select(record => new TopRevenueParkingLot(
            lots[record.ParkingLotId], record.Revenue, record.CompletedVehicleCount,
            totals.TotalRevenue == 0 ? 0 : Math.Round(record.Revenue / totals.TotalRevenue * 100, 2, MidpointRounding.AwayFromZero))).ToArray();
        return new BusinessReportSummary(query.ReportingPeriod, totals.TotalRevenue, totals.TotalVehicleCount,
            totals.AverageRevenuePerVehicle, topLots);
    }
}
