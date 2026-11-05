using System.Globalization;
using Microsoft.Data.Sqlite;
using ParkingBusinessResult.Application.Features.BusinessReports.Common;
using ParkingBusinessResult.Application.Features.BusinessReports.Queries.List;

namespace ParkingBusinessResult.Infrastructure.Repositories;

public sealed class SqliteBusinessReportMetadataReader(string databasePath) : IBusinessReportMetadataReader
{
    public IReadOnlyDictionary<Guid, BusinessReportMetadata> FindMany(IReadOnlySet<Guid> resultIds)
    {
        if (resultIds.Count == 0) return new Dictionary<Guid, BusinessReportMetadata>();
        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = databasePath, Mode = SqliteOpenMode.ReadWrite }.ToString());
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT r.id,p.Id,p.ma,p.ten,p.diaChi,u.tenDonVi,
                   r.ngayTao,r.nguoiTao,r.idNguoiTao,r.ngayCapNhat,r.nguoiCapNhat,r.idNguoiCapNhat
            FROM "SP_ketQuaKinhDoanhBaiDoXe" r
            JOIN "DL_KTHT_baiDoXe" p ON p.Id=r.baiDoXeId
            LEFT JOIN "SP_donViKinhDoanhQuanLy" u ON u.id=p.donViQuanLyId
            WHERE r.daXoa=0
            """;
        using SqliteDataReader reader = command.ExecuteReader();
        var result = new Dictionary<Guid, BusinessReportMetadata>();
        while (reader.Read())
        {
            if (!Guid.TryParse(reader.GetString(0), out Guid id) || !resultIds.Contains(id)) continue;
            var lot = new ReportParkingLot(Guid.Parse(reader.GetString(1)), Text(reader, 2), Text(reader, 3), Text(reader, 4));
            result.Add(id, new(lot, Text(reader, 5), Date(reader, 6), Text(reader, 7), Text(reader, 8),
                Date(reader, 9), Text(reader, 10), Text(reader, 11)));
        }
        return result;
    }

    private static string? Text(SqliteDataReader reader, int index) => reader.IsDBNull(index) ? null : reader.GetString(index);
    private static string? Date(SqliteDataReader reader, int index) =>
        DateTimeOffset.TryParse(Text(reader, index), CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset date)
            ? date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : null;
}
