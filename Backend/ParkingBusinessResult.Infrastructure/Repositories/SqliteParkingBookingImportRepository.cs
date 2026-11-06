using System.Globalization;
using Microsoft.Data.Sqlite;
using ParkingBusinessResult.Application.Features.BusinessResults.Common;
using ParkingBusinessResult.Application.Features.ParkingBookings.Common;

namespace ParkingBusinessResult.Infrastructure.Repositories;

public sealed class SqliteParkingBookingImportRepository(string databasePath) : IParkingBookingImportRepository
{
    public IParkingBookingImportSession BeginImport() => new ImportSession(databasePath);

    private sealed class ImportSession : IParkingBookingImportSession
    {
        private readonly SqliteConnection connection;
        private readonly SqliteTransaction transaction;

        public ImportSession(string databasePath)
        {
            connection = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = databasePath, Mode = SqliteOpenMode.ReadWrite, ForeignKeys = true, DefaultTimeout = 15
            }.ToString());
            try { connection.Open(); transaction = connection.BeginTransaction(); }
            catch { connection.Dispose(); throw; }
        }

        public Guid? FindAccessibleParkingLot(string code, BusinessAccess access)
        {
            using SqliteCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "SELECT Id FROM DL_KTHT_baiDoXe WHERE ma = $code COLLATE NOCASE LIMIT 2";
            command.Parameters.AddWithValue("$code", code);
            using SqliteDataReader reader = command.ExecuteReader();
            if (!reader.Read()) return null;
            Guid id = Guid.Parse(reader.GetString(0));
            return reader.Read() || !access.CanAccess(id) ? null : id;
        }

        public ParkingBookingSaved? TryInsert(ParkingBookingInput booking, string? userId, string? username)
        {
            using SqliteCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO SP_datTruocChoDoXe
                    (id,baiDoXeId,bienSoXe,loaiXe,thoiGianBatDau,thoiGianKetThuc,tongTien,soLuongChoDat,
                     ngayTao,ngayCapNhat,idNguoiTao,idNguoiCapNhat,nguoiTao,nguoiCapNhat)
                VALUES ($id,$lot,$plate,$type,$entered,$exited,$amount,1,$now,$now,$user,$user,$name,$name)
                ON CONFLICT (baiDoXeId,bienSoXe,thoiGianBatDau) DO NOTHING
                RETURNING id,bienSoXe,loaiXe,thoiGianBatDau,thoiGianKetThuc,tongTien
                """;
            command.Parameters.AddWithValue("$id", Guid.NewGuid().ToString());
            command.Parameters.AddWithValue("$lot", booking.ParkingLotId.ToString());
            command.Parameters.AddWithValue("$plate", booking.LicensePlate);
            command.Parameters.AddWithValue("$type", booking.VehicleType);
            command.Parameters.AddWithValue("$entered", booking.EnteredAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("$exited", (object?)booking.ExitedAt?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ?? DBNull.Value);
            command.Parameters.AddWithValue("$amount", (object?)booking.CollectedAmount?.ToString(CultureInfo.InvariantCulture) ?? DBNull.Value);
            command.Parameters.AddWithValue("$now", DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("$user", (object?)userId ?? DBNull.Value);
            command.Parameters.AddWithValue("$name", (object?)username ?? DBNull.Value);
            using SqliteDataReader reader = command.ExecuteReader();
            if (!reader.Read()) return null;
            return new ParkingBookingSaved(Guid.Parse(reader.GetString(0)), reader.GetString(1), reader.GetString(2),
                DateTime.Parse(reader.GetString(3), CultureInfo.InvariantCulture),
                reader.IsDBNull(4) ? null : DateTime.Parse(reader.GetString(4), CultureInfo.InvariantCulture),
                reader.IsDBNull(5) ? null : Convert.ToDecimal(reader.GetValue(5), CultureInfo.InvariantCulture));
        }

        public void Commit() => transaction.Commit();

        public void Dispose()
        {
            transaction.Dispose();
            connection.Dispose();
        }
    }
}
