using Microsoft.EntityFrameworkCore.Design;

namespace ParkingBusinessResult.Infrastructure.Persistence;

public sealed class ParkingDesignTimeFactory : IDesignTimeDbContextFactory<ParkingDbContext>
{
    public ParkingDbContext CreateDbContext(string[] args)
    {
        string path = Environment.GetEnvironmentVariable("ParkingBusinessResult__DatabasePath")
            ?? throw new InvalidOperationException("Set ParkingBusinessResult__DatabasePath before using EF tools.");
        return new ParkingContextFactory(path).CreateDbContext();
    }
}
