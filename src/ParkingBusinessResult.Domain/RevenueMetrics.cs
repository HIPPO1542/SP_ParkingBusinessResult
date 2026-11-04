namespace ParkingBusinessResult.Domain;

public static class RevenueMetrics
{
    public static decimal AverageRevenuePerVehicle(decimal totalRevenue, long completedVehicleCount)
    {
        if (completedVehicleCount <= 0)
        {
            return 0m;
        }

        return Math.Round(totalRevenue / completedVehicleCount, 2, MidpointRounding.AwayFromZero);
    }
}
