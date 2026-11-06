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
            SELECT v.id, v.baiDoXeId, v.bienSoXe, v.loaiXe, v.thoiGianBatDau, v.thoiGianKetThuc,
                   v.tongTien, p.ma, p.ten
            FROM "SP_datTruocChoDoXe" v JOIN "DL_KTHT_baiDoXe" p ON p.Id = v.baiDoXeId
            """;
        using SqliteDataReader reader = command.ExecuteReader();
        var visits = new List<ParkingVisitItem>();
        while (reader.Read())
        {
            visits.Add(new(Guid.Parse(reader.GetString(0)), Guid.Parse(reader.GetString(1)), reader.GetString(2), reader.GetString(3),
                DateTime.ParseExact(reader.GetString(4), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                reader.IsDBNull(5) ? null : DateTime.ParseExact(reader.GetString(5), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                reader.IsDBNull(6) ? null : Convert.ToDecimal(reader.GetValue(6), CultureInfo.InvariantCulture),
                reader.IsDBNull(7) ? null : reader.GetString(7), reader.IsDBNull(8) ? null : reader.GetString(8)));
        }
        return visits;
    }

    public IReadOnlyList<CompletedParkingVisit> ListCompleted(DateTime fromInclusive, DateTime toExclusive)
    {
        using SqliteConnection connection = Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT v.baiDoXeId, v.tongTien, p.ma, p.ten, p.diaChi
            FROM "SP_datTruocChoDoXe" v JOIN "DL_KTHT_baiDoXe" p ON p.Id = v.baiDoXeId
            WHERE v.thoiGianKetThuc >= $from AND v.thoiGianKetThuc < $to
            """;
        command.Parameters.AddWithValue("$from", fromInclusive.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$to", toExclusive.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
        using SqliteDataReader reader = command.ExecuteReader();
        var visits = new List<CompletedParkingVisit>();
        while (reader.Read())
            visits.Add(new(Guid.Parse(reader.GetString(0)), reader.IsDBNull(1) ? null : Convert.ToDecimal(reader.GetValue(1), CultureInfo.InvariantCulture),
                reader.IsDBNull(2) ? null : reader.GetString(2), reader.IsDBNull(3) ? null : reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetString(4)));
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
            INSERT INTO "SP_datTruocChoDoXe" (id, baiDoXeId, bienSoXe, loaiXe, thoiGianBatDau, thoiGianKetThuc, tongTien, soLuongChoDat, ngayTao, ngayCapNhat)
            VALUES ($id, $lot, $plate, $type, $entered, $exited, $amount, 1, $now, $now)
            ON CONFLICT (baiDoXeId, bienSoXe, thoiGianBatDau) DO NOTHING
            RETURNING id, bienSoXe, loaiXe, thoiGianBatDau, thoiGianKetThuc, tongTien
            """;
        command.Parameters.AddWithValue("$id", Guid.NewGuid().ToString());
        command.Parameters.AddWithValue("$lot", visit.ParkingLotId.ToString());
        command.Parameters.AddWithValue("$plate", visit.LicensePlate);
        command.Parameters.AddWithValue("$type", visit.VehicleType);
        command.Parameters.AddWithValue("$entered", visit.EnteredAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$exited", (object?)visit.ExitedAt?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ?? DBNull.Value);
        command.Parameters.AddWithValue("$amount", (object?)visit.CollectedAmount?.ToString(CultureInfo.InvariantCulture) ?? DBNull.Value);
        command.Parameters.AddWithValue("$now", DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture));
        using SqliteDataReader reader = command.ExecuteReader();
        if (!reader.Read()) return null;
        return new ParkingVisitSaved(Guid.Parse(reader.GetString(0)), reader.GetString(1), reader.GetString(2),
            DateTime.ParseExact(reader.GetString(3), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            reader.IsDBNull(4) ? null : DateTime.ParseExact(reader.GetString(4), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            reader.IsDBNull(5) ? null : Convert.ToDecimal(reader.GetValue(5), CultureInfo.InvariantCulture));
    }

    private SqliteConnection Open()
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = databasePath, Mode = SqliteOpenMode.ReadWrite, ForeignKeys = true }.ToString());
        connection.Open();
        return connection;
    }
}
