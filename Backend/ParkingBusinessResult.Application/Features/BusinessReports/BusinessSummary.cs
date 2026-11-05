namespace ParkingBusinessResult.Application.Features.BusinessReports;

public sealed record BusinessSummary(
    decimal TotalRevenue,
    long TotalVehicleCount,
    decimal AverageRevenuePerVehicle);
