using ParkingBusinessResult.Application.Features.BusinessReports.Common;

namespace ParkingBusinessResult.Application.Features.BusinessReports.Queries.Summary;

public sealed record TopRevenueParkingLot(ReportParkingLot ParkingLot, decimal Revenue, int VehicleCount, decimal Percentage);
public sealed record BusinessReportDistrict(string? District, decimal Revenue, long VehicleCount, int ParkingLotsCount, decimal Percentage);

public sealed record BusinessReportSummary(
    string ReportingPeriod,
    decimal TotalRevenue,
    long TotalVehicleCount,
    decimal AverageRevenuePerVehicle,
    decimal? GrowthRate,
    decimal? AverageDailyRevenue,
    IReadOnlyList<TopRevenueParkingLot> TopRevenueParkingLots,
    IReadOnlyList<BusinessReportDistrict> RevenueByDistrict);
