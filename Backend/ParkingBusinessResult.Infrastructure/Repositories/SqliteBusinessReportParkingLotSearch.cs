using Microsoft.Data.Sqlite;
using ParkingBusinessResult.Application.Features.BusinessReports.Common;

namespace ParkingBusinessResult.Infrastructure.Repositories;

public sealed class SqliteBusinessReportParkingLotSearch(string databasePath) : IBusinessReportParkingLotSearch
{
    public IReadOnlySet<Guid> FindMatchingLots(string keyword)
    {
        var connectionString = new SqliteConnectionStringBuilder { DataSource = databasePath, Mode = SqliteOpenMode.ReadWrite, ForeignKeys = true };
        using var connection = new SqliteConnection(connectionString.ToString());
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT \"Id\", \"ten\", \"diaChi\" FROM \"DL_KTHT_baiDoXe\"";
        using SqliteDataReader reader = command.ExecuteReader();
        var matchingLots = new HashSet<Guid>();
        while (reader.Read())
        {
            string name = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
            string address = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
            if (name.Contains(keyword, StringComparison.OrdinalIgnoreCase) || address.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                matchingLots.Add(Guid.Parse(reader.GetString(0)));
        }
        return matchingLots;
    }
}
