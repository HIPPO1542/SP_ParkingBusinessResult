using ParkingBusinessResult.Domain;

namespace ParkingBusinessResult.Application.Features.BusinessReports.Common;

public static class BusinessSummaryCalculator
{
    public static BusinessSummary Calculate(IEnumerable<BusinessSummaryEntry> entries)
    {
        decimal totalRevenue = 0;
        long totalVehicleCount = 0;

        foreach (BusinessSummaryEntry entry in entries)
        {
            totalRevenue += entry.Revenue;
            totalVehicleCount = checked(totalVehicleCount + entry.CompletedVehicleCount);
        }

        decimal averageRevenue = RevenueMetrics.AverageRevenuePerVehicle(totalRevenue, totalVehicleCount);
        return new BusinessSummary(totalRevenue, totalVehicleCount, averageRevenue);
    }
}
