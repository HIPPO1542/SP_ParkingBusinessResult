using ParkingBusinessResult.Application.Features.BusinessReports.Common;

namespace ParkingBusinessResult.Application.Features.BusinessReports.Queries.Summary;

public sealed record TopRevenueParkingLot(ReportParkingLot ParkingLot, decimal Revenue, int VehicleCount, decimal Percentage);

public sealed record BusinessReportSummary(
    string ReportingPeriod,
    decimal TotalRevenue,
    long TotalVehicleCount,
    decimal AverageRevenuePerVehicle,
    IReadOnlyList<TopRevenueParkingLot> TopRevenueParkingLots);
