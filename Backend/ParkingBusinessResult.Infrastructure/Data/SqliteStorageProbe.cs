using Microsoft.Data.Sqlite;

namespace ParkingBusinessResult.Infrastructure.Data;

public sealed class SqliteStorageProbe(string databasePath)
{
    public bool IsReady()
    {
        try
        {
            using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = databasePath, Mode = SqliteOpenMode.ReadOnly, DefaultTimeout = 3 }.ToString());
            connection.Open();
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name IN ('SP_datTruocChoDoXe','SP_ketQuaKinhDoanhBaiDoXe','DL_KTHT_baiDoXe')";
            return (long)command.ExecuteScalar()! == 3;
        }
        catch (SqliteException) { return false; }
    }
}
