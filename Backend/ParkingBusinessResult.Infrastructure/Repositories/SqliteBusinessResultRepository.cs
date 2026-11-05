using Microsoft.Data.Sqlite;
using ParkingBusinessResult.Domain;

namespace ParkingBusinessResult.Infrastructure;

public sealed class SqliteBusinessResultRepository : IBusinessResultRepository
{
    private readonly string connectionString;

    static SqliteBusinessResultRepository() => SQLitePCL.Batteries_V2.Init();

    public SqliteBusinessResultRepository(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWrite,
            ForeignKeys = true
        }.ToString();
    }

    public bool TryAdd(ParkingBusinessResultRecord businessResult)
    {
        ArgumentNullException.ThrowIfNull(businessResult);
        using SqliteConnection connection = OpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO "SP_ketQuaKinhDoanhBaiDoXe"
                ("id", "baiDoXeId", "kyBaoCao", "doanhThu", "soLuotXe", "ghiChu")
            VALUES ($id, $lot, $period, $revenue, $vehicles, $note)
            """;
        command.Parameters.AddWithValue("$id", businessResult.Id.ToString());
        command.Parameters.AddWithValue("$lot", businessResult.ParkingLotId.ToString());
        command.Parameters.AddWithValue("$period", businessResult.ReportingPeriod);
        SetValues(command, businessResult.Revenue, businessResult.CompletedVehicleCount, businessResult.Note);
        try
        {
            command.ExecuteNonQuery();
            return true;
        }
        catch (SqliteException exception) when (exception.SqliteExtendedErrorCode is 1555 or 2067)
        {
            return false;
        }
        catch (SqliteException exception) when (exception.SqliteExtendedErrorCode == 787)
        {
            throw new ParkingLotNotFoundException();
        }
    }

    public ParkingBusinessResultRecord? FindById(Guid id)
    {
        using SqliteConnection connection = OpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = SelectColumns + " WHERE \"id\" = $id AND \"daXoa\" = 0";
        command.Parameters.AddWithValue("$id", id.ToString());
        using SqliteDataReader reader = command.ExecuteReader();
        return reader.Read() ? ReadRecord(reader) : null;
    }

    public IReadOnlyList<ParkingBusinessResultRecord> List()
    {
        using SqliteConnection connection = OpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = SelectColumns + " WHERE \"daXoa\" = 0";
        using SqliteDataReader reader = command.ExecuteReader();
        var records = new List<ParkingBusinessResultRecord>();
        while (reader.Read())
        {
            records.Add(ReadRecord(reader));
        }
        return records;
    }

    public bool TryUpdate(Guid id, decimal revenue, int completedVehicleCount, string? note)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(revenue);
        ArgumentOutOfRangeException.ThrowIfNegative(completedVehicleCount);
        using SqliteConnection connection = OpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            UPDATE "SP_ketQuaKinhDoanhBaiDoXe"
            SET "doanhThu" = $revenue, "soLuotXe" = $vehicles, "ghiChu" = $note
            WHERE "id" = $id AND "daXoa" = 0
            """;
        command.Parameters.AddWithValue("$id", id.ToString());
        SetValues(command, revenue, completedVehicleCount, note);
        return command.ExecuteNonQuery() != 0;
    }

    public bool TrySoftDelete(Guid id)
    {
        using SqliteConnection connection = OpenConnection();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "UPDATE \"SP_ketQuaKinhDoanhBaiDoXe\" SET \"daXoa\" = 1 WHERE \"id\" = $id AND \"daXoa\" = 0";
        command.Parameters.AddWithValue("$id", id.ToString());
        return command.ExecuteNonQuery() != 0;
    }

    private const string SelectColumns = """
        SELECT "id", "baiDoXeId", "kyBaoCao", "doanhThu", "soLuotXe", "ghiChu"
        FROM "SP_ketQuaKinhDoanhBaiDoXe"
        """;

    private SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(connectionString);
        connection.Open();
        return connection;
    }

    private static void SetValues(SqliteCommand command, decimal revenue, int vehicleCount, string? note)
    {
        command.Parameters.AddWithValue("$revenue", (double)revenue);
        command.Parameters.AddWithValue("$vehicles", vehicleCount);
        command.Parameters.AddWithValue("$note", (object?)note ?? DBNull.Value);
    }

    private static ParkingBusinessResultRecord ReadRecord(SqliteDataReader reader) =>
        new(Guid.Parse(reader.GetString(0)), Guid.Parse(reader.GetString(1)), reader.GetString(2),
            Convert.ToDecimal(reader.GetDouble(3)), reader.GetInt32(4), reader.IsDBNull(5) ? null : reader.GetString(5));
}
