using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ParkingBusinessResult.Infrastructure.Data;

public sealed class ParkingContextFactory : IDesignTimeDbContextFactory<ParkingDbContext>
{
    public ParkingDbContext CreateDbContext(string[] args)
    {
        string path = Environment.GetEnvironmentVariable("ParkingBusinessResult__DatabasePath")
            ?? throw new InvalidOperationException("Set ParkingBusinessResult__DatabasePath to the SQLite database path before using EF tools.");
        return Create(path);
    }

    public static ParkingDbContext Create(string databasePath)
    {
        var connection = new Microsoft.Data.Sqlite.SqliteConnection(new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder
        {
            DataSource = databasePath, ForeignKeys = true, DefaultTimeout = 15
        }.ToString());
        connection.CreateCollation("ORDINAL_NOCASE", StringComparer.OrdinalIgnoreCase.Compare);
        connection.CreateFunction<string?, string, bool>("contains_ignore_case", (value, keyword) => value?.Contains(keyword, StringComparison.OrdinalIgnoreCase) == true, isDeterministic: true);
        return new(new DbContextOptionsBuilder<ParkingDbContext>().UseSqlite(connection, contextOwnsConnection: true).Options);
    }
}
