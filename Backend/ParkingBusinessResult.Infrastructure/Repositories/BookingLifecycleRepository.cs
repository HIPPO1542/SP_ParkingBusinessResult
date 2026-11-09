using System.Globalization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ParkingBusinessResult.Application.Features.BusinessReports.Common;
using ParkingBusinessResult.Application.Features.BusinessResults.Common;
using ParkingBusinessResult.Application.Features.ParkingBookings.Common;
using ParkingBusinessResult.Domain;
using ParkingBusinessResult.Domain.ValueObjects;
using ParkingBusinessResult.Infrastructure.Persistence;
using ParkingBusinessResult.Infrastructure.Persistence.Models;

namespace ParkingBusinessResult.Infrastructure.Repositories;

public sealed class BookingLifecycleRepository(IDbContextFactory<ParkingDbContext> contextFactory)
    : IBookingLifecycleRepository, ICustomerBookingQueries
{
    public BookingOptions GetOptions()
    {
        using var db = contextFactory.CreateDbContext();
        var lots = db.DlKthtBaiDoXe.AsNoTracking().OrderBy(row => row.Ma)
            .Select(row => new { row.Id, row.Ma, row.Ten, row.DiaChi }).ToArray();
        string[] types = db.SpChiTietMucThuKhungGia.AsNoTracking().Where(row => row.LoaiPhuongTien != null)
            .Select(row => row.LoaiPhuongTien!).Distinct().OrderBy(type => type).ToArray();
        return new(lots.Select(row => new ReportParkingLot(Guid.Parse(row.Id), row.Ma, row.Ten, row.DiaChi)).ToArray(), types);
    }

    public CustomerBookingPage GetMyBookings(string customerId, int pageNumber, int pageSize)
    {
        using var db = contextFactory.CreateDbContext();
        using var snapshot = db.Database.BeginTransaction();
        var query = db.SpDatTruocChoDoXe.AsNoTracking().Where(row => row.IdNguoiTao == customerId);
        int count = query.Count();
        long offset = ((long)pageNumber - 1) * pageSize;
        BookingDetails[] rows = offset > int.MaxValue ? [] : query.Include(row => row.BaiDoXe)
            .OrderByDescending(row => row.NgayTao).ThenBy(row => row.Id).Skip((int)offset).Take(pageSize)
            .AsEnumerable().Select(Details).ToArray();
        snapshot.Commit();
        return new(rows, count, pageNumber, pageSize);
    }

    public BookingEstimateResult Estimate(BookingEstimateInput input)
    {
        using var db = contextFactory.CreateDbContext();
        if (!db.DlKthtBaiDoXe.Any(row => row.Id == input.ParkingLotId.ToString()))
            return new(null, "Bãi xe không tồn tại.");
        return Estimate(db, input);
    }

    private static BookingEstimateResult Estimate(ParkingDbContext db, BookingEstimateInput input)
    {
        string dayType = input.ExpectedArrivalAt.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ? "WEEKEND" : "WEEKDAY";
        var rules = BookingTariffRepository.ReadRules(db, input.ParkingLotId.ToString(), input.VehicleType,
            input.ExpectedArrivalAt, input.ExpectedDepartureAt, dayType);
        if (rules.Count != 1) return new(null, "Không tìm thấy duy nhất một mức giá dự kiến phù hợp với bãi, loại xe và khoảng thời gian đã chọn.");
        ParkingTariffRule rule = rules[0];
        try
        {
            ParkingFee fee = ParkingFee.Calculate(input.ExpectedArrivalAt, input.ExpectedDepartureAt,
                rule.InitialHours, rule.InitialPrice, rule.AdditionalHourPrice);
            return new(new(fee.BillableHours, fee.Amount, dayType), null);
        }
        catch (ArgumentException error) { return new(null, error.Message); }
    }

    public BookingWriteResult Reserve(ReservationInput input, AuditActor actor, DateTime now)
    {
        Booking booking;
        try { booking = Booking.Reserve(input.ParkingLotId, input.LicensePlate, input.VehicleType, input.ExpectedArrivalAt, input.ExpectedDepartureAt, now); }
        catch (ArgumentException error) { return new(null, error.Message); }
        using var db = contextFactory.CreateDbContext();
        using var transaction = db.Database.BeginTransaction();
        var lot = db.DlKthtBaiDoXe.SingleOrDefault(row => row.Id == input.ParkingLotId.ToString());
        if (lot is null) return new(null, "Bãi xe không tồn tại.");
        BookingEstimateResult estimate = Estimate(db, new(input.ParkingLotId, booking.VehicleType, booking.StartAt, booking.EndAt!.Value));
        if (estimate.Error is not null) return new(null, estimate.Error);
        string stamp = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
        var row = new SpDatTruocChoDoXe
        {
            Id = booking.Id.ToString(), BaiDoXeId = lot.Id, BaiDoXe = lot, BienSoXe = booking.LicensePlate,
            LoaiXe = booking.VehicleType, ThoiGianBatDau = booking.StartAt, ThoiGianKetThuc = booking.EndAt,
            TrangThaiDatCho = Booking.Reserved, TongTien = (double)estimate.Estimate!.Amount,
            SoLuongChoDat = 1, GhiChu = input.Note?.Trim(), IdNguoiTao = actor.Id, NguoiTao = actor.Name,
            IdNguoiCapNhat = actor.Id, NguoiCapNhat = actor.Name, NgayTao = stamp, NgayCapNhat = stamp
        };
        db.SpDatTruocChoDoXe.Add(row);
        try { db.SaveChanges(); }
        catch (DbUpdateException error) when (error.InnerException is SqliteException { SqliteExtendedErrorCode: 1555 or 2067 })
        { return new(null, "Đã có booking cùng bãi, biển số và giờ đến dự kiến."); }
        transaction.Commit();
        return new(Details(row), null);
    }

    public BookingWriteResult CheckIn(Guid id, BusinessAccess access, AuditActor actor, DateTime now)
    {
        using var db = contextFactory.CreateDbContext();
        using var transaction = db.Database.BeginTransaction();
        var row = db.SpDatTruocChoDoXe.Include(item => item.BaiDoXe).SingleOrDefault(item => item.Id == id.ToString());
        if (row is null || !access.CanAccess(Guid.Parse(row.BaiDoXeId))) return new(null, "Không tìm thấy booking hoặc không có quyền với bãi xe.");
        Booking booking = ToDomain(row);
        try { booking.CheckIn(now); }
        catch (InvalidOperationException error) { return new(null, error.Message); }
        bool alreadyParked = db.SpDatTruocChoDoXe.Any(other => other.Id != row.Id && other.BaiDoXeId == row.BaiDoXeId &&
            other.BienSoXe == row.BienSoXe && other.ThoiGianKetThuc == null && other.TrangThaiDatCho != Booking.Reserved);
        if (alreadyParked) return new(null, "Xe đang có một lượt gửi chưa hoàn tất tại bãi này.");
        Apply(row, booking, actor);
        db.SaveChanges();
        transaction.Commit();
        return new(Details(row), null);
    }

    public BookingWriteResult CheckOut(Guid id, string dayType, BusinessAccess access, AuditActor actor, DateTime now)
    {
        using var db = contextFactory.CreateDbContext();
        using var transaction = db.Database.BeginTransaction();
        var row = db.SpDatTruocChoDoXe.Include(item => item.BaiDoXe).SingleOrDefault(item => item.Id == id.ToString());
        if (row is null || !access.CanAccess(Guid.Parse(row.BaiDoXeId))) return new(null, "Không tìm thấy booking hoặc không có quyền với bãi xe.");
        Booking booking = ToDomain(row);
        if (booking.Status != Booking.CheckedIn) return new(null, "Chỉ lượt đang gửi mới được check-out.");
        var rules = BookingTariffRepository.ReadRules(db, row.BaiDoXeId, row.LoaiXe, booking.StartAt, now, dayType);
        if (rules.Count != 1) return new(null, "Không tìm thấy duy nhất một mức giá phù hợp với bãi, loại xe, loại ngày và thời gian hiệu lực. Chưa hoàn tất lượt gửi.");
        ParkingTariffRule rule = rules[0];
        try
        {
            ParkingFee fee = ParkingFee.Calculate(booking.StartAt, now, rule.InitialHours, rule.InitialPrice, rule.AdditionalHourPrice);
            booking.CheckOut(now, fee.Amount);
        }
        catch (ArgumentException error) { return new(null, error.Message); }
        Apply(row, booking, actor);
        db.SaveChanges();
        transaction.Commit();
        return new(Details(row), null);
    }

    private static Booking ToDomain(SpDatTruocChoDoXe row)
    {
        // Imported CONFIRMED rows already represent actual visits; keep their stored data unchanged.
        string status = row.TrangThaiDatCho == "CONFIRMED"
            ? row.ThoiGianKetThuc is null ? Booking.CheckedIn : Booking.Completed : row.TrangThaiDatCho;
        return new(Guid.Parse(row.Id), Guid.Parse(row.BaiDoXeId), row.BienSoXe, row.LoaiXe,
            row.ThoiGianBatDau, row.ThoiGianKetThuc, status, row.TongTien.HasValue ? Convert.ToDecimal(row.TongTien.Value) : null);
    }

    private static void Apply(SpDatTruocChoDoXe row, Booking booking, AuditActor actor)
    {
        row.ThoiGianBatDau = booking.StartAt;
        row.ThoiGianKetThuc = booking.EndAt;
        row.TrangThaiDatCho = booking.Status;
        row.TongTien = booking.Amount.HasValue ? (double)booking.Amount.Value : null;
        row.IdNguoiCapNhat = actor.Id;
        row.NguoiCapNhat = actor.Name;
        row.NgayCapNhat = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
    }

    private static BookingDetails Details(SpDatTruocChoDoXe row)
    {
        Booking booking = ToDomain(row);
        long? hours = booking.Status == Booking.Completed && booking.EndAt > booking.StartAt
            ? ParkingFee.RoundHours(booking.StartAt, booking.EndAt.Value) : null;
        return new(booking.Id, new(booking.ParkingLotId, row.BaiDoXe.Ma, row.BaiDoXe.Ten, row.BaiDoXe.DiaChi),
            booking.LicensePlate, booking.VehicleType, booking.StartAt, booking.EndAt, booking.Status,
            row.TrangThaiDatCho is Booking.Reserved or Booking.CheckedIn ? null : booking.Amount, hours, row.GhiChu,
            row.NgayTao, row.NguoiTao, row.IdNguoiTao, row.NgayCapNhat, row.NguoiCapNhat, row.IdNguoiCapNhat,
            row.TrangThaiDatCho is Booking.Reserved or Booking.CheckedIn ? booking.Amount : null);
    }
}
