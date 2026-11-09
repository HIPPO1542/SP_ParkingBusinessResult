using Microsoft.EntityFrameworkCore;
using ParkingBusinessResult.Infrastructure.Persistence;
using ParkingBusinessResult.Infrastructure.Persistence.Models;
using ParkingBusinessResult.Application.Features.BusinessResults.Common;
using ParkingBusinessResult.Application.Features.ParkingBookings.Common;
using ParkingBusinessResult.Application.Features.ParkingBookings.Queries.List;
using ParkingBusinessResult.Domain;

namespace ParkingBusinessResult.Infrastructure.Repositories;

public sealed class BookingQueryRepository(IDbContextFactory<ParkingDbContext> contextFactory) : IBookingListRepository, IBookingSummaryRepository
{
    public BookingPage GetPage(ListBookingsQuery query)
    {
        using var db = contextFactory.CreateDbContext();
        // COUNT and page are separate EF queries; this short read transaction keeps one database snapshot.
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
        snapshot.Commit();
        return new(items, total, query.PageNumber, query.PageSize);
    }

    public IReadOnlyList<string> GetVehicleTypes(BusinessAccess access)
    {
        using var db = contextFactory.CreateDbContext();
        return Scope(db.SpDatTruocChoDoXe.AsNoTracking(), access, null)
            .Select(row => EF.Functions.Collate(row.LoaiXe, "ORDINAL_NOCASE")).Distinct().OrderBy(type => type).ToArray();
    }

    public IReadOnlyList<BookingOverviewGroup> GetOverview(BusinessAccess access, int? day, int? month, int? year)
    {
        using var db = contextFactory.CreateDbContext();
        var rows = Scope(db.SpDatTruocChoDoXe.AsNoTracking(), access, null)
            .Where(row => row.TrangThaiDatCho == Booking.Reserved || row.ThoiGianKetThuc != null ||
                row.TrangThaiDatCho == Booking.CheckedIn || row.TrangThaiDatCho == "CONFIRMED");
        if (year.HasValue)
        {
            DateTime from = new(year.Value, month ?? 1, day ?? 1);
            DateTime lastDate = new(year.Value, month ?? 12, day ?? (month.HasValue ? DateTime.DaysInMonth(year.Value, month.Value) : 31));
            // Reservations still contain planned times; only completed visits use the actual exit time.
            rows = rows.Where(row => (row.TrangThaiDatCho != Booking.Reserved && row.ThoiGianKetThuc != null
                ? row.ThoiGianKetThuc.Value : row.ThoiGianBatDau) >= from);
            if (lastDate < DateTime.MaxValue.Date)
            {
                DateTime toExclusive = lastDate.AddDays(1);
                rows = rows.Where(row => (row.TrangThaiDatCho != Booking.Reserved && row.ThoiGianKetThuc != null
                    ? row.ThoiGianKetThuc.Value : row.ThoiGianBatDau) < toExclusive);
            }
        }
        return rows.GroupBy(row => new { row.BaiDoXeId, VehicleType = EF.Functions.Collate(row.LoaiXe, "ORDINAL_NOCASE") })
            .Select(group => new
            {
                group.Key.BaiDoXeId, group.Key.VehicleType,
                CompletedCount = group.LongCount(row => row.TrangThaiDatCho != Booking.Reserved && row.ThoiGianKetThuc != null),
                ParkedCount = group.LongCount(row => row.TrangThaiDatCho != Booking.Reserved && row.ThoiGianKetThuc == null),
                ReservedCount = group.LongCount(row => row.TrangThaiDatCho == Booking.Reserved),
                Revenue = group.Sum(row => row.TrangThaiDatCho != Booking.Reserved && row.ThoiGianKetThuc != null && row.TongTien != null ? (decimal)row.TongTien.Value : 0m),
                MissingAmountCount = group.LongCount(row => row.TrangThaiDatCho != Booking.Reserved && row.ThoiGianKetThuc != null && row.TongTien == null)
            }).AsEnumerable().Select(item => new BookingOverviewGroup(Guid.Parse(item.BaiDoXeId), item.VehicleType,
                item.CompletedCount, item.ParkedCount, item.ReservedCount, item.Revenue, item.MissingAmountCount)).ToArray();
    }

    public IReadOnlyList<CompletedBooking> ListCompleted(BookingSummaryFilter filter)
    {
        using var db = contextFactory.CreateDbContext();
        return ListCompleted(db, filter);
    }

    public BookingExportData ReadForExport(BookingSummaryFilter filter)
    {
        using var db = contextFactory.CreateDbContext();
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
