using Microsoft.EntityFrameworkCore;
using ParkingBusinessResult.Infrastructure.Persistence;
using ParkingBusinessResult.Infrastructure.Persistence.Models;
using ParkingBusinessResult.Application.Features.BusinessResults.Common;
using ParkingBusinessResult.Application.Features.ParkingBookings.Common;
using ParkingBusinessResult.Application.Features.ParkingBookings.Queries.List;
using ParkingBusinessResult.Domain;

namespace ParkingBusinessResult.Infrastructure.Repositories;

public sealed class BookingQueryRepository(ParkingDbContext db) : IBookingListRepository, IBookingSummaryRepository
{
    public BookingPage GetPage(ListBookingsQuery query)
    {
        // Count, page and overview share one database snapshot.
        using var snapshot = db.Database.BeginTransaction();
        var rows = Scope(db.SpDatTruocChoDoXe.AsNoTracking(), query.Access, query.ParkingLotId);
        if (query.Status == "RESERVED") rows = rows.Where(row => row.TrangThaiDatCho == Booking.Reserved);
        if (query.Status == "PARKED") rows = rows.Where(row => row.ThoiGianKetThuc == null && row.TrangThaiDatCho != Booking.Reserved);
        if (query.Status == "COMPLETED") rows = rows.Where(row => row.ThoiGianKetThuc != null && row.TrangThaiDatCho != Booking.Reserved);
        if (query.VehicleType is not null) rows = rows.Where(row => EF.Functions.Collate(row.LoaiXe, "ORDINAL_NOCASE") == query.VehicleType);
        bool useExit = query.DateField == "exitedAt";
        if (query.Day.HasValue) rows = rows.Where(row => (useExit ? row.ThoiGianKetThuc : row.ThoiGianBatDau)!.Value.Day == query.Day);
        if (query.Month.HasValue) rows = rows.Where(row => (useExit ? row.ThoiGianKetThuc : row.ThoiGianBatDau)!.Value.Month == query.Month);
        if (query.Year.HasValue) rows = rows.Where(row => (useExit ? row.ThoiGianKetThuc : row.ThoiGianBatDau)!.Value.Year == query.Year);
        var joined = rows.Join(db.DlKthtBaiDoXe, row => row.BaiDoXeId, lot => lot.Id, (row, lot) => new { Row = row, Lot = lot });
        int total = joined.Count();
        long offset = ((long)query.PageNumber - 1) * query.PageSize;
        var page = offset > int.MaxValue ? [] : joined.OrderByDescending(item => item.Row.ThoiGianBatDau).ThenBy(item => item.Row.Id)
            .Skip((int)offset).Take(query.PageSize).Select(item => new
            {
                item.Row.Id, item.Row.BaiDoXeId, item.Row.BienSoXe, item.Row.LoaiXe,
                item.Row.ThoiGianBatDau, item.Row.ThoiGianKetThuc, item.Row.TongTien, item.Row.TrangThaiDatCho,
                Code = item.Lot.Ma, Name = item.Lot.Ten
            }).ToArray();
        var items = page.Select(item => new BookingItem(Guid.Parse(item.Id), Guid.Parse(item.BaiDoXeId),
            item.BienSoXe, item.LoaiXe, item.ThoiGianBatDau, item.ThoiGianKetThuc,
            item.TrangThaiDatCho is Booking.Reserved or Booking.CheckedIn ? null : item.TongTien.HasValue ? Convert.ToDecimal(item.TongTien.Value) : null, item.Code, item.Name,
            item.TrangThaiDatCho == Booking.Reserved ? Booking.Reserved : item.ThoiGianKetThuc is null ? Booking.CheckedIn : Booking.Completed)).ToArray();
        BookingOverview overview = GetOverview(db, query.Access);
        snapshot.Commit();
        return new(items, total, query.PageNumber, query.PageSize, overview);
    }

    public IReadOnlyList<string> GetVehicleTypes(BusinessAccess access)
    {
        return Scope(db.SpDatTruocChoDoXe.AsNoTracking(), access, null)
            .Select(row => EF.Functions.Collate(row.LoaiXe, "ORDINAL_NOCASE")).Distinct().OrderBy(type => type).ToArray();
    }

    public BookingOverview GetOverview(BusinessAccess access)
    {
        return GetOverview(db, access);
    }

    private static BookingOverview GetOverview(ParkingDbContext db, BusinessAccess access)
    {
        // Overview covers all time and permitted lots, independently of list/statistics filters.
        var rows = Scope(db.SpDatTruocChoDoXe.AsNoTracking(), access, null)
            .Join(db.DlKthtBaiDoXe, row => row.BaiDoXeId, lot => lot.Id, (row, lot) => row);
        var totals = rows.GroupBy(row => 1).Select(group => new
        {
            Revenue = group.Sum(row => row.TrangThaiDatCho != Booking.Reserved && row.ThoiGianKetThuc != null && row.TongTien != null
                ? (decimal)row.TongTien.Value : 0m),
            Completed = group.LongCount(row => row.TrangThaiDatCho != Booking.Reserved && row.ThoiGianKetThuc != null),
            Parked = group.LongCount(row => row.ThoiGianKetThuc == null &&
                (row.TrangThaiDatCho == Booking.CheckedIn || row.TrangThaiDatCho == "CONFIRMED")),
            Reserved = group.LongCount(row => row.TrangThaiDatCho == Booking.Reserved),
            Missing = group.LongCount(row => row.TrangThaiDatCho != Booking.Reserved && row.ThoiGianKetThuc != null && row.TongTien == null)
        }).SingleOrDefault();
        return totals is null ? new(0, 0, 0, 0, 0) : new(totals.Revenue, totals.Completed, totals.Parked, totals.Reserved, totals.Missing);
    }

    public BookingOpenCounts GetOpenCounts(BookingSummaryFilter filter)
    {
        var rows = Scope(db.SpDatTruocChoDoXe.AsNoTracking(), filter.Access, filter.ParkingLotId)
            .Where(row => (row.TrangThaiDatCho == Booking.Reserved ||
                (row.ThoiGianKetThuc == null && (row.TrangThaiDatCho == Booking.CheckedIn || row.TrangThaiDatCho == "CONFIRMED"))) &&
                row.ThoiGianBatDau >= filter.FromInclusive && row.ThoiGianBatDau < filter.ToExclusive);
        if (filter.VehicleType is not null)
            rows = rows.Where(row => EF.Functions.Collate(row.LoaiXe, "ORDINAL_NOCASE") == filter.VehicleType);
        if (filter.ParkingLotIds is not null)
        {
            string[] selected = filter.ParkingLotIds.Select(id => id.ToString()).ToArray();
            rows = rows.Where(row => selected.Contains(row.BaiDoXeId));
        }
        var joined = rows.Join(db.DlKthtBaiDoXe, row => row.BaiDoXeId, lot => lot.Id, (row, lot) => new { Row = row, Lot = lot });
        if (!string.IsNullOrWhiteSpace(filter.District))
        {
            string district = filter.District.Trim();
            joined = joined.Where(item => ParkingDbContext.ContainsIgnoreCase(item.Lot.DiaChi, district));
        }
        var counts = joined.GroupBy(item => item.Row.TrangThaiDatCho == Booking.Reserved)
            .Select(group => new { IsReserved = group.Key, Count = group.LongCount() }).ToArray();
        return new(counts.FirstOrDefault(item => !item.IsReserved)?.Count ?? 0,
            counts.FirstOrDefault(item => item.IsReserved)?.Count ?? 0);
    }

    public IReadOnlyList<CompletedBooking> ListCompleted(BookingSummaryFilter filter)
    {
        return ListCompleted(db, filter);
    }

    public BookingExportData ReadForExport(BookingSummaryFilter filter)
    {
        using var snapshot = db.Database.BeginTransaction();
        var completed = ListCompleted(db, filter);
        var unfinished = ListUnfinishedCounts(db, filter);
        snapshot.Commit();
        return new(completed, unfinished);
    }

    private static IReadOnlyList<CompletedBooking> ListCompleted(ParkingDbContext db, BookingSummaryFilter filter)
    {
        var rows = Scope(db.SpDatTruocChoDoXe.AsNoTracking(), filter.Access, filter.ParkingLotId)
            .Where(row => row.TrangThaiDatCho != Booking.Reserved && row.ThoiGianKetThuc >= filter.FromInclusive && row.ThoiGianKetThuc < filter.ToExclusive);
        if (filter.VehicleType is not null)
            rows = rows.Where(row => EF.Functions.Collate(row.LoaiXe, "ORDINAL_NOCASE") == filter.VehicleType);
        if (filter.ParkingLotIds is not null)
        {
            string[] selected = filter.ParkingLotIds.Select(id => id.ToString()).ToArray();
            rows = rows.Where(row => selected.Contains(row.BaiDoXeId));
        }
        var joined = rows.Join(db.DlKthtBaiDoXe, row => row.BaiDoXeId, lot => lot.Id, (row, lot) => new { Row = row, Lot = lot });
        if (!string.IsNullOrWhiteSpace(filter.District))
        {
            string district = filter.District.Trim();
            joined = joined.Where(item => ParkingDbContext.ContainsIgnoreCase(item.Lot.DiaChi, district));
        }
        return joined.Select(item => new { item.Row.BaiDoXeId, item.Row.TongTien, item.Row.LoaiXe, item.Lot.Ma, item.Lot.Ten, item.Lot.DiaChi })
            .AsEnumerable().Select(item => new CompletedBooking(Guid.Parse(item.BaiDoXeId),
                item.TongTien.HasValue ? Convert.ToDecimal(item.TongTien.Value) : null, item.Ma, item.Ten, item.DiaChi, item.LoaiXe)).ToArray();
    }

    private static IReadOnlyList<UnfinishedBookingCount> ListUnfinishedCounts(ParkingDbContext db, BookingSummaryFilter filter)
    {
        var rows = Scope(db.SpDatTruocChoDoXe.AsNoTracking(), filter.Access, filter.ParkingLotId)
            .Where(row => row.ThoiGianKetThuc == null &&
                (row.TrangThaiDatCho == Booking.CheckedIn || row.TrangThaiDatCho == "CONFIRMED") &&
                row.ThoiGianBatDau >= filter.FromInclusive && row.ThoiGianBatDau < filter.ToExclusive);
        if (filter.VehicleType is not null)
            rows = rows.Where(row => EF.Functions.Collate(row.LoaiXe, "ORDINAL_NOCASE") == filter.VehicleType);
        if (filter.ParkingLotIds is not null)
        {
            string[] selected = filter.ParkingLotIds.Select(id => id.ToString()).ToArray();
            rows = rows.Where(row => selected.Contains(row.BaiDoXeId));
        }
        var joined = rows.Join(db.DlKthtBaiDoXe, row => row.BaiDoXeId, lot => lot.Id, (row, lot) => new { Row = row, Lot = lot });
        if (!string.IsNullOrWhiteSpace(filter.District))
        {
            string district = filter.District.Trim();
            joined = joined.Where(item => ParkingDbContext.ContainsIgnoreCase(item.Lot.DiaChi, district));
        }
        return joined.GroupBy(item => new { item.Row.BaiDoXeId, VehicleType = EF.Functions.Collate(item.Row.LoaiXe, "ORDINAL_NOCASE"), item.Lot.Ma, item.Lot.Ten })
            .Select(group => new { group.Key.BaiDoXeId, group.Key.VehicleType, group.Key.Ma, group.Key.Ten, Count = group.Count() })
            .AsEnumerable().Select(item => new UnfinishedBookingCount(Guid.Parse(item.BaiDoXeId), item.VehicleType, item.Count, item.Ma, item.Ten)).ToArray();
    }

    private static IQueryable<SpDatTruocChoDoXe> Scope(IQueryable<SpDatTruocChoDoXe> rows, BusinessAccess access, Guid? lot)
    {
        if (!access.IsAdministrator)
        {
            string[] allowed = access.ParkingLotIds.Select(id => id.ToString()).ToArray();
            rows = rows.Where(row => allowed.Contains(row.BaiDoXeId));
        }
        if (lot.HasValue) { string id = lot.Value.ToString(); rows = rows.Where(row => row.BaiDoXeId == id); }
        return rows;
    }
}
