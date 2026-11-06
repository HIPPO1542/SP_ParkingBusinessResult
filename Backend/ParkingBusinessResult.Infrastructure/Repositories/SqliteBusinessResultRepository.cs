using System.Globalization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ParkingBusinessResult.Domain;
using ParkingBusinessResult.Infrastructure.Data;
using ParkingBusinessResult.Infrastructure.Data.Models;

namespace ParkingBusinessResult.Infrastructure;

public sealed class SqliteBusinessResultRepository(string databasePath) : IBusinessResultRepository
{
    public bool TryAdd(ParkingBusinessResultRecord record, AuditActor? actor = null)
    {
        using var db = ParkingContextFactory.Create(databasePath);
        string now = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
        db.SpKetQuaKinhDoanhBaiDoXe.Add(new()
        {
            Id = record.Id.ToString(), BaiDoXeId = record.ParkingLotId.ToString(), KyBaoCao = record.ReportingPeriod,
            DoanhThu = (double)record.Revenue, SoLuotXe = record.CompletedVehicleCount, GhiChu = record.Note,
            IdNguoiTao = actor?.Id, NguoiTao = actor?.Name, NgayTao = now,
            IdNguoiCapNhat = actor?.Id, NguoiCapNhat = actor?.Name, NgayCapNhat = now
        });
        try { db.SaveChanges(); return true; }
        catch (DbUpdateException error) when (error.InnerException is SqliteException { SqliteExtendedErrorCode: 1555 or 2067 }) { return false; }
        catch (DbUpdateException error) when (error.InnerException is SqliteException { SqliteExtendedErrorCode: 787 }) { throw new ParkingLotNotFoundException(); }
    }

    public ParkingBusinessResultRecord? FindById(Guid id)
    {
        using var db = ParkingContextFactory.Create(databasePath);
        var row = db.SpKetQuaKinhDoanhBaiDoXe.AsNoTracking().SingleOrDefault(row => row.Id == id.ToString() && row.DaXoa == 0);
        return row is null ? null : Map(row);
    }

    public IReadOnlyList<ParkingBusinessResultRecord> List()
    {
        using var db = ParkingContextFactory.Create(databasePath);
        return db.SpKetQuaKinhDoanhBaiDoXe.AsNoTracking().Where(row => row.DaXoa == 0).AsEnumerable().Select(Map).ToArray();
    }

    public bool TryUpdate(Guid id, decimal revenue, int completedVehicleCount, string? note, AuditActor? actor = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(revenue);
        ArgumentOutOfRangeException.ThrowIfNegative(completedVehicleCount);
        using var db = ParkingContextFactory.Create(databasePath);
        string now = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
        return db.SpKetQuaKinhDoanhBaiDoXe.Where(row => row.Id == id.ToString() && row.DaXoa == 0)
            .ExecuteUpdate(setters => setters.SetProperty(row => row.DoanhThu, (double)revenue)
                .SetProperty(row => row.SoLuotXe, completedVehicleCount).SetProperty(row => row.GhiChu, note)
                .SetProperty(row => row.IdNguoiCapNhat, actor == null ? null : actor.Id)
                .SetProperty(row => row.NguoiCapNhat, actor == null ? null : actor.Name).SetProperty(row => row.NgayCapNhat, now)) != 0;
    }

    public bool TrySoftDelete(Guid id, AuditActor? actor = null)
    {
        using var db = ParkingContextFactory.Create(databasePath);
        string now = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
        return db.SpKetQuaKinhDoanhBaiDoXe.Where(row => row.Id == id.ToString() && row.DaXoa == 0)
            .ExecuteUpdate(setters => setters.SetProperty(row => row.DaXoa, 1)
                .SetProperty(row => row.IdNguoiCapNhat, actor == null ? null : actor.Id)
                .SetProperty(row => row.NguoiCapNhat, actor == null ? null : actor.Name).SetProperty(row => row.NgayCapNhat, now)) != 0;
    }

    private static ParkingBusinessResultRecord Map(SpKetQuaKinhDoanhBaiDoXe row) => new(Guid.Parse(row.Id), Guid.Parse(row.BaiDoXeId),
        row.KyBaoCao, Convert.ToDecimal(row.DoanhThu), row.SoLuotXe, row.GhiChu);
}
