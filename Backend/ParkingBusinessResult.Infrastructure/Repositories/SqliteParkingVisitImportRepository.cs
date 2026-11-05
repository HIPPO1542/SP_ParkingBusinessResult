using System.Globalization;
using Microsoft.Data.Sqlite;
using ParkingBusinessResult.Application.Features.BusinessResults.Common;
using ParkingBusinessResult.Application.Features.ParkingVisits.Common;

namespace ParkingBusinessResult.Infrastructure.Repositories;

public sealed class SqliteParkingVisitImportRepository(string databasePath) : IParkingVisitImportRepository, IParkingVisitSummaryRepository, IParkingVisitListRepository
{
    public IReadOnlyList<ParkingVisitItem> List()
    {
        using SqliteConnection connection = Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT v.id, v.parkingLotId, v.licensePlate, v.vehicleType, v.enteredAt, v.exitedAt,
                   v.collectedAmount, v.paymentMethod, p.ma, p.ten
            FROM parking_visit v JOIN "DL_KTHT_baiDoXe" p ON p.Id = v.parkingLotId
            """;
        using SqliteDataReader reader = command.ExecuteReader();
        var visits = new List<ParkingVisitItem>();
        while (reader.Read())
        {
            visits.Add(new(Guid.Parse(reader.GetString(0)), Guid.Parse(reader.GetString(1)), reader.GetString(2), reader.GetString(3),
                DateTime.ParseExact(reader.GetString(4), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                reader.IsDBNull(5) ? null : DateTime.ParseExact(reader.GetString(5), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                reader.IsDBNull(6) ? null : decimal.Parse(reader.GetString(6), CultureInfo.InvariantCulture),
                reader.IsDBNull(7) ? null : reader.GetString(7),
                reader.IsDBNull(8) ? null : reader.GetString(8), reader.IsDBNull(9) ? null : reader.GetString(9)));
        }
        return visits;
    }

    public IReadOnlyList<CompletedParkingVisit> ListCompleted(DateTime fromInclusive, DateTime toExclusive)
    {
        using SqliteConnection connection = Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT v.parkingLotId, v.collectedAmount, v.paymentMethod, p.ma, p.ten, p.diaChi
            FROM parking_visit v JOIN "DL_KTHT_baiDoXe" p ON p.Id = v.parkingLotId
            WHERE v.exitedAt >= $from AND v.exitedAt < $to
            """;
        command.Parameters.AddWithValue("$from", fromInclusive.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$to", toExclusive.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
        using SqliteDataReader reader = command.ExecuteReader();
        var visits = new List<CompletedParkingVisit>();
        while (reader.Read())
            visits.Add(new(Guid.Parse(reader.GetString(0)), reader.IsDBNull(1) ? null : decimal.Parse(reader.GetString(1), CultureInfo.InvariantCulture),
                reader.IsDBNull(2) ? null : reader.GetString(2), reader.IsDBNull(3) ? null : reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetString(4), reader.IsDBNull(5) ? null : reader.GetString(5)));
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

    public ParkingVisitSaved? TryInsert(ParkingVisitInput visit)
    {
        using SqliteConnection connection = Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO parking_visit (id, parkingLotId, licensePlate, vehicleType, enteredAt, exitedAt, collectedAmount, paymentMethod, createdAt, updatedAt)
            VALUES ($id, $lot, $plate, $type, $entered, $exited, $amount, $method, $now, $now)
            ON CONFLICT (parkingLotId, licensePlate, enteredAt) DO NOTHING
            RETURNING id, licensePlate, vehicleType, enteredAt, exitedAt, collectedAmount, paymentMethod
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
        using SqliteDataReader reader = command.ExecuteReader();
        if (!reader.Read()) return null;
        return new ParkingVisitSaved(Guid.Parse(reader.GetString(0)), reader.GetString(1), reader.GetString(2),
            DateTime.ParseExact(reader.GetString(3), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            reader.IsDBNull(4) ? null : DateTime.ParseExact(reader.GetString(4), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            reader.IsDBNull(5) ? null : decimal.Parse(reader.GetString(5), CultureInfo.InvariantCulture),
            reader.IsDBNull(6) ? null : reader.GetString(6));
    }

    private SqliteConnection Open()
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = databasePath, Mode = SqliteOpenMode.ReadWrite, ForeignKeys = true }.ToString());
        connection.Open();
        return connection;
    }
}
