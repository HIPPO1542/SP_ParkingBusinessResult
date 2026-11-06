using Microsoft.Data.Sqlite;
using ParkingBusinessResult.Application.Features.LocalAccounts.Common;

namespace ParkingBusinessResult.Infrastructure.Repositories;

public sealed class SqliteLocalAccountRepository : ILocalAccountRepository
{
    private readonly string connectionString;

    public SqliteLocalAccountRepository(string databasePath)
    {
        connectionString = new SqliteConnectionStringBuilder { DataSource = databasePath, Mode = SqliteOpenMode.ReadWrite }.ToString();
    }

    public void Initialize()
    {
        using SqliteConnection connection = Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS "dev_auth_users" (
                "id" TEXT NOT NULL PRIMARY KEY,
                "username" TEXT NOT NULL COLLATE NOCASE UNIQUE,
                "passwordHash" TEXT NOT NULL,
                "role" TEXT NOT NULL,
                "parkingLotId" TEXT
            )
            """;
        command.ExecuteNonQuery();
    }

    public void AddFirstAdministrator(LocalAccount account)
    {
        using SqliteConnection connection = Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO dev_auth_users (id, username, passwordHash, role)
            SELECT $id, $username, $hash, $role
            WHERE NOT EXISTS (SELECT 1 FROM dev_auth_users)
            """;
        AddAccountParameters(command, account);
        command.ExecuteNonQuery();
    }

    public bool TryAdd(LocalAccount account)
    {
        using SqliteConnection connection = Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "INSERT INTO dev_auth_users (id, username, passwordHash, role, parkingLotId) VALUES ($id, $username, $hash, $role, $lot)";
        AddAccountParameters(command, account);
        command.Parameters.AddWithValue("$lot", (object?)account.ParkingLotId?.ToString() ?? DBNull.Value);
        try { command.ExecuteNonQuery(); return true; }
        catch (SqliteException exception) when (exception.SqliteExtendedErrorCode is 1555 or 2067) { return false; }
    }

    public LocalAccount? FindByUsername(string username)
    {
        using SqliteConnection connection = Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT id, username, passwordHash, role, parkingLotId FROM dev_auth_users WHERE username = $username";
        command.Parameters.AddWithValue("$username", username);
        using SqliteDataReader reader = command.ExecuteReader();
        if (!reader.Read()) return null;
        Guid? parkingLotId = reader.IsDBNull(4) ? null : Guid.Parse(reader.GetString(4));
        return new LocalAccount(Guid.Parse(reader.GetString(0)), reader.GetString(1), reader.GetString(2), reader.GetString(3), parkingLotId);
    }

    private SqliteConnection Open()
    {
        var connection = new SqliteConnection(connectionString);
        connection.Open();
        return connection;
    }

    private static void AddAccountParameters(SqliteCommand command, LocalAccount account)
    {
        command.Parameters.AddWithValue("$id", account.Id.ToString());
        command.Parameters.AddWithValue("$username", account.Username);
        command.Parameters.AddWithValue("$hash", account.PasswordHash);
        command.Parameters.AddWithValue("$role", account.Role);
    }
}
