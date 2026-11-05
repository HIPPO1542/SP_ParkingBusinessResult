using System.Globalization;
using Microsoft.Data.Sqlite;
using ParkingBusinessResult.Application.Features.BusinessResults.Common;
using ParkingBusinessResult.Application.Features.ParkingVisits.Common;

namespace ParkingBusinessResult.Infrastructure.Repositories;

public sealed class SqliteParkingVisitImportRepository(string databasePath) : IParkingVisitImportRepository, IParkingVisitSummaryRepository
{
    public IReadOnlyList<CompletedParkingVisit> ListCompleted(DateTime fromInclusive, DateTime toExclusive)
    {
        using SqliteConnection connection = Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT parkingLotId, collectedAmount FROM parking_visit WHERE exitedAt >= $from AND exitedAt < $to";
        command.Parameters.AddWithValue("$from", fromInclusive.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$to", toExclusive.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
        using SqliteDataReader reader = command.ExecuteReader();
        var visits = new List<CompletedParkingVisit>();
        while (reader.Read())
            visits.Add(new(Guid.Parse(reader.GetString(0)), reader.IsDBNull(1) ? null : decimal.Parse(reader.GetString(1), CultureInfo.InvariantCulture)));
        return visits;
    }

    public Guid? FindAccessibleParkingLot(string code, BusinessAccess access)
    {
        using SqliteConnection connection = Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT \"Id\" FROM \"DL_KTHT_baiDoXe\" WHERE \"ma\" = $code COLLATE NOCASE LIMIT 2";
        command.Parameters.AddWithValue("$code", code);
        using SqliteDataReader reader = command.ExecuteReader();
        if (!reader.Read()) return null;
        Guid id = Guid.Parse(reader.GetString(0));
        if (reader.Read() || !access.CanAccess(id)) return null;
        return id;
    }

    public Guid Upsert(ParkingVisitInput visit)
    {
        using SqliteConnection connection = Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO parking_visit (id, parkingLotId, licensePlate, vehicleType, enteredAt, exitedAt, collectedAmount, paymentMethod, createdAt, updatedAt)
            VALUES ($id, $lot, $plate, $type, $entered, $exited, $amount, $method, $now, $now)
            ON CONFLICT (parkingLotId, licensePlate, enteredAt) DO UPDATE SET
                vehicleType = excluded.vehicleType,
                exitedAt = COALESCE(excluded.exitedAt, parking_visit.exitedAt),
                collectedAmount = COALESCE(excluded.collectedAmount, parking_visit.collectedAmount),
                paymentMethod = COALESCE(excluded.paymentMethod, parking_visit.paymentMethod),
                updatedAt = excluded.updatedAt
            RETURNING id
            """;
        command.Parameters.AddWithValue("$id", Guid.NewGuid().ToString());
        command.Parameters.AddWithValue("$lot", visit.ParkingLotId.ToString());
        command.Parameters.AddWithValue("$plate", visit.LicensePlate);
        command.Parameters.AddWithValue("$type", visit.VehicleType);
        command.Parameters.AddWithValue("$entered", visit.EnteredAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$exited", (object?)visit.ExitedAt?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ?? DBNull.Value);
        command.Parameters.AddWithValue("$amount", (object?)visit.CollectedAmount?.ToString(CultureInfo.InvariantCulture) ?? DBNull.Value);
        command.Parameters.AddWithValue("$method", (object?)visit.PaymentMethod ?? DBNull.Value);
        command.Parameters.AddWithValue("$now", DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture));
        return Guid.Parse((string)command.ExecuteScalar()!);
    }

    private SqliteConnection Open()
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = databasePath, Mode = SqliteOpenMode.ReadWrite, ForeignKeys = true }.ToString());
        connection.Open();
        return connection;
    }
}
