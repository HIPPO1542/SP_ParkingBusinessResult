using System.Globalization;
using Microsoft.Data.Sqlite;
using ParkingBusinessResult.Application.Features.BusinessResults.Common;
using ParkingBusinessResult.Application.Features.ParkingVisits.Common;

namespace ParkingBusinessResult.Infrastructure.Repositories;

public sealed class SqliteParkingVisitTariffRepository(string databasePath) : IParkingVisitTariffRepository
{
    public ParkingVisitTariffData? Find(Guid visitId, string dayType, BusinessAccess access)
    {
        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = databasePath, Mode = SqliteOpenMode.ReadWrite, ForeignKeys = true }.ToString());
        connection.Open();
        using SqliteCommand visitCommand = connection.CreateCommand();
        visitCommand.CommandText = "SELECT baiDoXeId, loaiXe, thoiGianBatDau, thoiGianKetThuc, tongTien FROM \"SP_datTruocChoDoXe\" WHERE id = $id";
        visitCommand.Parameters.AddWithValue("$id", visitId.ToString());
        using SqliteDataReader reader = visitCommand.ExecuteReader();
        if (!reader.Read()) return null;
        Guid lotId = Guid.Parse(reader.GetString(0));
        if (!access.CanAccess(lotId)) return null;
        string vehicleType = reader.GetString(1);
        string enteredText = reader.GetString(2);
        string? exitedText = reader.IsDBNull(3) ? null : reader.GetString(3);
        DateTime entered = DateTime.ParseExact(enteredText, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        DateTime? exited = exitedText is null ? null : DateTime.ParseExact(exitedText, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        decimal? collected = reader.IsDBNull(4) ? null : Convert.ToDecimal(reader.GetValue(4), CultureInfo.InvariantCulture);
        reader.Close();
        var rules = new List<ParkingTariffRule>();
        if (exitedText is null) return new(entered, null, collected, rules);
        using SqliteCommand tableCheck = connection.CreateCommand();
        tableCheck.CommandText = "SELECT count(*) FROM sqlite_master WHERE type='table' AND name IN ('SP_khungGiaThuPhi','SP_chiTietMucThuKhungGia','SP_apDungKhungGia')";
        if ((long)tableCheck.ExecuteScalar()! != 3) return new(entered, exited, collected, rules);
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT r.soGioDau, r.giaGioDau, r.giaGioTiepTheo
            FROM "SP_chiTietMucThuKhungGia" r
            JOIN "SP_khungGiaThuPhi" f ON f.id = r.idKhungGia
            JOIN "SP_apDungKhungGia" a ON a.khungGiaId = f.id AND a.baiDoXeId = f.baiDoXeId
            WHERE a.baiDoXeId = $lot AND a.trangThaiApDung = 'APPLIED' AND f.trangThai = 'ACTIVE'
              AND julianday(a.ngayApDung) <= julianday($entered)
              AND julianday(f.ngayHieuLuc) <= julianday($entered) AND julianday(f.ngayHetHieuLuc) >= julianday($exited)
              AND r.loaiPhuongTien = $type AND r.loaiNgay = $day
            """;
        command.Parameters.AddWithValue("$lot", lotId.ToString());
        command.Parameters.AddWithValue("$entered", enteredText);
        command.Parameters.AddWithValue("$exited", exitedText);
        command.Parameters.AddWithValue("$type", vehicleType);
        command.Parameters.AddWithValue("$day", dayType);
        using SqliteDataReader ruleReader = command.ExecuteReader();
        while (ruleReader.Read())
            rules.Add(new(Convert.ToDecimal(ruleReader.GetDouble(0)), Convert.ToDecimal(ruleReader.GetDouble(1)), Convert.ToDecimal(ruleReader.GetDouble(2))));
        return new(entered, exited, collected, rules);
    }
}
