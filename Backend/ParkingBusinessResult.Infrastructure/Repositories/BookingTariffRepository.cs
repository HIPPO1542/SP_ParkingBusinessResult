using System.Globalization;
using Microsoft.EntityFrameworkCore;
using ParkingBusinessResult.Infrastructure.Persistence;
using ParkingBusinessResult.Application.Features.BusinessResults.Common;
using ParkingBusinessResult.Application.Features.ParkingBookings.Common;
namespace ParkingBusinessResult.Infrastructure.Repositories;
public sealed class BookingTariffRepository(IDbContextFactory<ParkingDbContext> contextFactory) : IBookingTariffRepository
{
    public BookingTariffData? Find(Guid bookingId, string dayType, BusinessAccess access)
    {
        using var db = contextFactory.CreateDbContext();
        var booking = db.SpDatTruocChoDoXe.AsNoTracking().SingleOrDefault(row => row.Id == bookingId.ToString());
        if (booking is null || !access.CanAccess(Guid.Parse(booking.BaiDoXeId))) return null;
        decimal? amount = booking.TongTien.HasValue ? Convert.ToDecimal(booking.TongTien.Value) : null;
        var rules = new List<ParkingTariffRule>();
        if (booking.ThoiGianKetThuc is null) return new(booking.ThoiGianBatDau, null, amount, rules);
        int tables = db.Database.SqlQueryRaw<int>("SELECT count(*) AS Value FROM sqlite_master WHERE type='table' AND name IN ('SP_khungGiaThuPhi','SP_chiTietMucThuKhungGia','SP_apDungKhungGia')").Single();
        if (tables == 3)
        {
            string entered = booking.ThoiGianBatDau.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            string exited = booking.ThoiGianKetThuc.Value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            // julianday preserves the existing SQLite interpretation of price effective dates.
            var prices = db.Database.SqlQuery<TariffRow>($"""
                SELECT r.soGioDau AS FirstHours,r.giaGioDau AS FirstPrice,r.giaGioTiepTheo AS NextPrice
                FROM SP_chiTietMucThuKhungGia r JOIN SP_khungGiaThuPhi f ON f.id=r.idKhungGia
                JOIN SP_apDungKhungGia a ON a.khungGiaId=f.id AND a.baiDoXeId=f.baiDoXeId
                WHERE a.baiDoXeId={booking.BaiDoXeId} AND a.trangThaiApDung='APPLIED' AND f.trangThai='ACTIVE'
                AND julianday(a.ngayApDung)<=julianday({entered}) AND julianday(f.ngayHieuLuc)<=julianday({entered})
                AND julianday(f.ngayHetHieuLuc)>=julianday({exited}) AND r.loaiPhuongTien={booking.LoaiXe} AND r.loaiNgay={dayType}
                """).ToArray();
            rules.AddRange(prices.Select(row => new ParkingTariffRule(Convert.ToDecimal(row.FirstHours), Convert.ToDecimal(row.FirstPrice), Convert.ToDecimal(row.NextPrice))));
        }
        return new(booking.ThoiGianBatDau, booking.ThoiGianKetThuc, amount, rules);
    }
    private sealed class TariffRow
    {
        public double FirstHours { get; set; }
        public double FirstPrice { get; set; }
        public double NextPrice { get; set; }
    }
}
