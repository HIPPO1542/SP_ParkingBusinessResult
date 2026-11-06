using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using ParkingBusinessResult.Infrastructure.Data;
using ParkingBusinessResult.Application.Features.BusinessResults.Common;
using ParkingBusinessResult.Application.Features.ParkingBookings.Common;

namespace ParkingBusinessResult.Infrastructure.Repositories;

public sealed class SqliteParkingBookingImportRepository(string databasePath) : IParkingBookingImportRepository
{
    public IParkingBookingImportSession BeginImport() => new ImportSession(databasePath);

    private sealed class ImportSession : IParkingBookingImportSession
    {
        private readonly ParkingDbContext db;
        private readonly IDbContextTransaction transaction;

        public ImportSession(string path)
        {
            db = ParkingContextFactory.Create(path);
            try { transaction = db.Database.BeginTransaction(); }
            catch { db.Dispose(); throw; }
        }

        public Guid? FindAccessibleParkingLot(string code, BusinessAccess access)
        {
            var ids = db.DlKthtBaiDoXe.AsNoTracking().Where(lot => EF.Functions.Collate(lot.Ma!, "NOCASE") == code)
                .Select(lot => lot.Id).Take(2).ToArray();
            return ids.Length == 1 && Guid.TryParse(ids[0], out var id) && access.CanAccess(id) ? id : null;
        }

        public ParkingBookingSaved? TryInsert(ParkingBookingInput booking, string? userId, string? username)
        {
            string id = Guid.NewGuid().ToString();
            string entered = booking.EnteredAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            string? exited = booking.ExitedAt?.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            string now = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
            double? amount = booking.CollectedAmount.HasValue ? (double)booking.CollectedAmount.Value : null;
            // SQLite-specific conflict handling keeps duplicate rows from aborting the batch transaction.
            int inserted = db.Database.ExecuteSqlInterpolated($"""
                INSERT INTO SP_datTruocChoDoXe
                    (id,baiDoXeId,bienSoXe,loaiXe,thoiGianBatDau,thoiGianKetThuc,tongTien,soLuongChoDat,
                     ngayTao,ngayCapNhat,idNguoiTao,idNguoiCapNhat,nguoiTao,nguoiCapNhat)
                VALUES ({id},{booking.ParkingLotId.ToString()},{booking.LicensePlate},{booking.VehicleType},{entered},{exited},{amount},1,
                        {now},{now},{userId},{userId},{username},{username})
                ON CONFLICT (baiDoXeId,bienSoXe,thoiGianBatDau) DO NOTHING
                """);
            return inserted == 0 ? null : new(Guid.Parse(id), booking.LicensePlate, booking.VehicleType,
                booking.EnteredAt, booking.ExitedAt, amount.HasValue ? Convert.ToDecimal(amount.Value) : null);
        }

        public void Commit() => transaction.Commit();
        public void Dispose() { transaction.Dispose(); db.Dispose(); }
    }
}
