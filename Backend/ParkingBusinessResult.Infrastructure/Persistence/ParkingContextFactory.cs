using Microsoft.EntityFrameworkCore;

namespace ParkingBusinessResult.Infrastructure.Persistence;

public sealed class ParkingContextFactory(string databasePath) : IDbContextFactory<ParkingDbContext>
{
    public ParkingDbContext CreateDbContext()
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
