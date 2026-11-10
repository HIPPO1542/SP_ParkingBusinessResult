using System.Globalization;
using Microsoft.EntityFrameworkCore;
using ParkingBusinessResult.Infrastructure.Persistence;
using ParkingBusinessResult.Application.Features.BusinessResults.Common;
using ParkingBusinessResult.Application.Features.ParkingBookings.Common;
using ParkingBusinessResult.Domain;

namespace ParkingBusinessResult.Infrastructure.Repositories;

public sealed class BookingTariffRepository(ParkingDbContext db) : IBookingTariffRepository
{
    public BookingTariffData? Find(Guid bookingId, string dayType, BusinessAccess access)
    {
        var booking = db.SpDatTruocChoDoXe.AsNoTracking().SingleOrDefault(row => row.Id == bookingId.ToString());
        if (booking is null || booking.TrangThaiDatCho == Booking.Cancelled || !access.CanAccess(Guid.Parse(booking.BaiDoXeId))) return null;
        decimal? amount = booking.TongTien.HasValue ? Convert.ToDecimal(booking.TongTien.Value) : null;
        IReadOnlyList<ParkingTariffRule> rules = booking.ThoiGianKetThuc is null ? [] :
            ReadRules(db, booking.BaiDoXeId, booking.LoaiXe, booking.ThoiGianBatDau, booking.ThoiGianKetThuc.Value, dayType);
        return new(booking.ThoiGianBatDau, booking.ThoiGianKetThuc, amount, rules);
    }

    internal static IReadOnlyList<ParkingTariffRule> ReadRules(ParkingDbContext db, string lotId, string vehicleType,
        DateTime enteredAt, DateTime exitedAt, string dayType)
    {
        int tables = db.Database.SqlQueryRaw<int>("SELECT count(*) AS Value FROM sqlite_master WHERE type='table' AND name IN ('SP_khungGiaThuPhi','SP_chiTietMucThuKhungGia','SP_apDungKhungGia')").Single();
        if (tables != 3) return [];
        string entered = enteredAt.ToString("yyyy-MM-dd HH:mm:ss.fffffff", CultureInfo.InvariantCulture);
        string exited = exitedAt.ToString("yyyy-MM-dd HH:mm:ss.fffffff", CultureInfo.InvariantCulture);
        var prices = db.Database.SqlQuery<TariffRow>($"""
            SELECT r.soGioDau AS FirstHours,r.giaGioDau AS FirstPrice,r.giaGioTiepTheo AS NextPrice
            FROM SP_chiTietMucThuKhungGia r JOIN SP_khungGiaThuPhi f ON f.id=r.idKhungGia
            JOIN SP_apDungKhungGia a ON a.khungGiaId=f.id AND a.baiDoXeId=f.baiDoXeId
            WHERE a.baiDoXeId={lotId} AND a.trangThaiApDung='APPLIED' AND f.trangThai='ACTIVE'
            AND julianday(a.ngayApDung)<=julianday({entered}) AND julianday(f.ngayHieuLuc)<=julianday({entered})
            AND julianday(f.ngayHetHieuLuc)>=julianday({exited}) AND r.loaiPhuongTien={vehicleType} AND r.loaiNgay={dayType}
            """).ToArray();
        return prices.Select(row => new ParkingTariffRule(Convert.ToDecimal(row.FirstHours),
            Convert.ToDecimal(row.FirstPrice), Convert.ToDecimal(row.NextPrice))).ToArray();
    }

    private sealed class TariffRow
    {
        public double FirstHours { get; set; }
        public double FirstPrice { get; set; }
        public double NextPrice { get; set; }
    }
}
