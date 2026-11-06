using System.Globalization;
using System.Text;
using Microsoft.Data.Sqlite;
using ParkingBusinessResult.Application.Features.BusinessResults.Common;
using ParkingBusinessResult.Application.Features.ParkingBookings.Common;
using ParkingBusinessResult.Application.Features.ParkingBookings.Queries.List;

namespace ParkingBusinessResult.Infrastructure.Repositories;

public sealed class SqliteParkingBookingQueryRepository(string databasePath) : IParkingBookingListRepository, IParkingBookingSummaryRepository
{
    public ParkingBookingPage GetPage(ListParkingBookingsQuery query)
    {
        using SqliteConnection connection = Open();
        using SqliteCommand command = connection.CreateCommand();
        var where = new StringBuilder(" WHERE 1=1");
        AddScope(command, where, query.Access, query.ParkingLotId);
        if (query.Status is not null)
            where.Append(query.Status == "PARKED" ? " AND v.thoiGianKetThuc IS NULL" : " AND v.thoiGianKetThuc IS NOT NULL");
        if (query.VehicleType is not null)
        {
            where.Append(" AND v.loaiXe = $type COLLATE ORDINAL_NOCASE");
            command.Parameters.AddWithValue("$type", query.VehicleType);
        }
        string dateColumn = query.DateField == "exitedAt" ? "v.thoiGianKetThuc" : "v.thoiGianBatDau";
        AddDatePart(command, where, dateColumn, "%d", "$day", query.Day);
        AddDatePart(command, where, dateColumn, "%m", "$month", query.Month);
        AddDatePart(command, where, dateColumn, "%Y", "$year", query.Year);
        // Count and page share one statement snapshot, including an empty requested page.
        command.CommandText = $"""
            WITH filtered AS (
                SELECT v.id,v.baiDoXeId,v.bienSoXe,v.loaiXe,v.thoiGianBatDau,v.thoiGianKetThuc,v.tongTien,p.ma,p.ten
                FROM SP_datTruocChoDoXe v JOIN DL_KTHT_baiDoXe p ON p.Id=v.baiDoXeId {where}
            ), page AS (
                SELECT * FROM filtered ORDER BY thoiGianBatDau DESC,id LIMIT $limit OFFSET $offset
            )
            SELECT (SELECT COUNT(*) FROM filtered),page.* FROM (SELECT 1) LEFT JOIN page ON 1=1
            ORDER BY page.thoiGianBatDau DESC,page.id
            """;
        command.Parameters.AddWithValue("$limit", query.PageSize);
        command.Parameters.AddWithValue("$offset", ((long)query.PageNumber - 1) * query.PageSize);
        var items = new List<ParkingBookingItem>();
        int total = 0;
        using SqliteDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            total = checked((int)reader.GetInt64(0));
            if (reader.IsDBNull(1)) continue;
            items.Add(new(Guid.Parse(reader.GetString(1)), Guid.Parse(reader.GetString(2)), reader.GetString(3), reader.GetString(4),
                DateTime.Parse(reader.GetString(5), CultureInfo.InvariantCulture),
                reader.IsDBNull(6) ? null : DateTime.Parse(reader.GetString(6), CultureInfo.InvariantCulture),
                reader.IsDBNull(7) ? null : Convert.ToDecimal(reader.GetValue(7), CultureInfo.InvariantCulture), Text(reader, 8), Text(reader, 9)));
        }
        return new(items, total, query.PageNumber, query.PageSize);
    }

    public IReadOnlyList<string> GetVehicleTypes(BusinessAccess access)
    {
        using SqliteConnection connection = Open();
        using SqliteCommand command = connection.CreateCommand();
        var where = new StringBuilder(" WHERE 1=1");
        AddScope(command, where, access, null);
        command.CommandText = $"SELECT DISTINCT v.loaiXe COLLATE ORDINAL_NOCASE FROM SP_datTruocChoDoXe v {where} ORDER BY v.loaiXe COLLATE ORDINAL_NOCASE";
        using SqliteDataReader reader = command.ExecuteReader();
        var types = new List<string>();
        while (reader.Read()) types.Add(reader.GetString(0));
        return types;
    }

    public IReadOnlyList<CompletedParkingBooking> ListCompleted(ParkingBookingSummaryFilter filter)
    {
        using SqliteConnection connection = Open();
        using SqliteCommand command = connection.CreateCommand();
        var where = new StringBuilder(" WHERE v.thoiGianKetThuc >= $from AND v.thoiGianKetThuc < $to");
        command.Parameters.AddWithValue("$from", filter.FromInclusive.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$to", filter.ToExclusive.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
        AddScope(command, where, filter.Access, filter.ParkingLotId);
        if (filter.ParkingLotIds is not null) AddIds(command, where, filter.ParkingLotIds, "selected");
        if (!string.IsNullOrWhiteSpace(filter.District))
        {
            where.Append(" AND contains_ignore_case(p.diaChi,$district)");
            command.Parameters.AddWithValue("$district", filter.District.Trim());
        }
        command.CommandText = $"SELECT v.baiDoXeId,v.tongTien,p.ma,p.ten,p.diaChi FROM SP_datTruocChoDoXe v JOIN DL_KTHT_baiDoXe p ON p.Id=v.baiDoXeId {where}";
        using SqliteDataReader reader = command.ExecuteReader();
        var bookings = new List<CompletedParkingBooking>();
        while (reader.Read()) bookings.Add(new(Guid.Parse(reader.GetString(0)),
            reader.IsDBNull(1) ? null : Convert.ToDecimal(reader.GetValue(1), CultureInfo.InvariantCulture), Text(reader, 2), Text(reader, 3), Text(reader, 4)));
        return bookings;
    }

    private static void AddDatePart(SqliteCommand command, StringBuilder where, string column, string format, string parameter, int? value)
    {
        if (!value.HasValue) return;
        where.Append($" AND CAST(strftime('{format}',{column}) AS INTEGER) = {parameter}");
        command.Parameters.AddWithValue(parameter, value.Value);
    }

    private static void AddScope(SqliteCommand command, StringBuilder where, BusinessAccess access, Guid? lot)
    {
        if (!access.IsAdministrator) AddIds(command, where, access.ParkingLotIds, "allowed");
        if (!lot.HasValue) return;
        where.Append(" AND v.baiDoXeId=$lot");
        command.Parameters.AddWithValue("$lot", lot.Value.ToString());
    }

    private static void AddIds(SqliteCommand command, StringBuilder where, IReadOnlySet<Guid> ids, string prefix)
    {
        if (ids.Count == 0) { where.Append(" AND 0=1"); return; }
        var parameters = new List<string>();
        foreach (Guid id in ids)
        {
            string parameter = $"${prefix}{parameters.Count}";
            parameters.Add(parameter);
            command.Parameters.AddWithValue(parameter, id.ToString());
        }
        where.Append($" AND v.baiDoXeId IN ({string.Join(',', parameters)})");
    }

    private SqliteConnection Open()
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = databasePath, Mode = SqliteOpenMode.ReadOnly, DefaultTimeout = 15 }.ToString());
        try
        {
            connection.Open();
            connection.CreateCollation("ORDINAL_NOCASE", StringComparer.OrdinalIgnoreCase.Compare);
            connection.CreateFunction<string?, string, bool>("contains_ignore_case", (value, keyword) => value?.Contains(keyword, StringComparison.OrdinalIgnoreCase) == true, isDeterministic: true);
            return connection;
        }
        catch { connection.Dispose(); throw; }
    }

    private static string? Text(SqliteDataReader reader, int column) => reader.IsDBNull(column) ? null : reader.GetString(column);
}
