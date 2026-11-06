using ParkingBusinessResult.Application.Common;
using ParkingBusinessResult.Application.Features.BusinessReports.Queries.List;
using ParkingBusinessResult.Application.Features.BusinessReports.Common;
using ParkingBusinessResult.Domain;
using ParkingBusinessResult.Domain.ValueObjects;

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
            records.Select(record => record.ParkingLotId).ToHashSet());
        TopRevenueParkingLot[] topLots = topRecords.Select(record => new TopRevenueParkingLot(
            lots[record.ParkingLotId], record.Revenue, record.CompletedVehicleCount,
            totals.TotalRevenue == 0 ? 0 : Math.Round(record.Revenue / totals.TotalRevenue * 100, 1, MidpointRounding.AwayFromZero))).ToArray();
        BusinessReportingPeriod? period = BusinessReportingPeriod.Parse(query.ReportingPeriod);
        decimal? averageDailyRevenue = period is null ? null : Math.Round(totals.TotalRevenue / period.DaysInPeriod, 2, MidpointRounding.AwayFromZero);
        decimal? growthRate = null;
        string? previousPeriod = period?.PreviousPeriodValue();
        if (previousPeriod is not null && records.Count > 0)
        {
            IReadOnlyList<ParkingBusinessResultRecord> previousRecords = listHandler.Handle(
                new ListBusinessReportQuery(previousPeriod, query.ParkingLotId, query.Access, District: query.District));
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
        return new BusinessReportSummary(query.ReportingPeriod, totals.TotalRevenue, totals.TotalVehicleCount,
            totals.AverageRevenuePerVehicle, growthRate, averageDailyRevenue, topLots, districts);
    }

}
