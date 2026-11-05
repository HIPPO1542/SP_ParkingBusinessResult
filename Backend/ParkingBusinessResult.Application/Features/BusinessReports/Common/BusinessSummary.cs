namespace ParkingBusinessResult.Application.Features.BusinessReports.Common;

public sealed record BusinessSummary(
    decimal TotalRevenue,
    long TotalVehicleCount,
    decimal AverageRevenuePerVehicle);
