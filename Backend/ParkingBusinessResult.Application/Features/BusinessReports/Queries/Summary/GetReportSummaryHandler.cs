using ParkingBusinessResult.Application.Common;
using ParkingBusinessResult.Application.Features.BusinessReports.Queries.List;
using ParkingBusinessResult.Application.Features.BusinessReports.Common;
using ParkingBusinessResult.Domain;
using ParkingBusinessResult.Domain.ValueObjects;

namespace ParkingBusinessResult.Application.Features.BusinessReports.Queries.Summary;

public sealed class GetReportSummaryHandler(ListReportsHandler listHandler, IReportParkingLotSearch parkingLotSearch)
{
    public ReportSummary Handle(GetReportSummaryQuery query)
    {
        IReadOnlyList<BusinessResultRecord> records = listHandler.Handle(
            new ListReportsQuery(query.ReportingPeriod, query.ParkingLotId, query.Access, District: query.District));
        BusinessSummary totals = BusinessSummaryCalculator.Calculate(records.Select(record =>
            new BusinessSummaryEntry(record.Revenue, record.CompletedVehicleCount)));
        BusinessResultRecord[] topRecords = records.OrderByDescending(record => record.Revenue)
            .ThenBy(record => record.ParkingLotId).Take(5).ToArray();
        IReadOnlyDictionary<Guid, ReportParkingLot> lots = parkingLotSearch.GetParkingLots(
            records.Select(record => record.ParkingLotId).ToHashSet());
        TopRevenueParkingLot[] topLots = topRecords.Select(record => new TopRevenueParkingLot(
            lots[record.ParkingLotId], record.Revenue, record.CompletedVehicleCount,
            totals.TotalRevenue == 0 ? 0 : Math.Round(record.Revenue / totals.TotalRevenue * 100, 1, MidpointRounding.AwayFromZero))).ToArray();
        ReportPeriod? period = ReportPeriod.Parse(query.ReportingPeriod);
        decimal? averageDailyRevenue = period is null ? null : Math.Round(totals.TotalRevenue / period.DaysInPeriod, 2, MidpointRounding.AwayFromZero);
        decimal? growthRate = null;
        string? previousPeriod = period?.PreviousPeriodValue();
        if (previousPeriod is not null && records.Count > 0)
        {
            IReadOnlyList<BusinessResultRecord> previousRecords = listHandler.Handle(
                new ListReportsQuery(previousPeriod, query.ParkingLotId, query.Access, District: query.District));
            bool sameLots = records.Select(record => record.ParkingLotId).ToHashSet().SetEquals(previousRecords.Select(record => record.ParkingLotId));
            decimal previousRevenue = previousRecords.Sum(record => record.Revenue);
            if (sameLots && previousRevenue > 0)
                growthRate = Math.Round((totals.TotalRevenue - previousRevenue) / previousRevenue * 100, 2, MidpointRounding.AwayFromZero);
        }
        BusinessReportDistrict[] districts = records.GroupBy(record => DistrictNames.FromAddress(lots[record.ParkingLotId].Address) ?? "", StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                decimal revenue = group.Sum(record => record.Revenue);
                decimal percentage = totals.TotalRevenue == 0 ? 0 : Math.Round(revenue / totals.TotalRevenue * 100, 1, MidpointRounding.AwayFromZero);
                return new BusinessReportDistrict(group.Key.Length == 0 ? null : group.Key, revenue,
                    group.Sum(record => (long)record.CompletedVehicleCount), group.Select(record => record.ParkingLotId).Distinct().Count(), percentage);
            }).OrderByDescending(group => group.Revenue).ThenBy(group => group.District, StringComparer.OrdinalIgnoreCase).ToArray();
        return new ReportSummary(query.ReportingPeriod, totals.TotalRevenue, totals.TotalVehicleCount,
            totals.AverageRevenuePerVehicle, growthRate, averageDailyRevenue, topLots, districts);
    }

}
