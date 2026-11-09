using System.Globalization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ParkingBusinessResult.Domain;
using ParkingBusinessResult.Infrastructure.Persistence;
using ParkingBusinessResult.Infrastructure.Persistence.Models;

namespace ParkingBusinessResult.Infrastructure;

public sealed class BusinessResultRepository(ParkingDbContext db) : IBusinessResultRepository
{
    public bool TryAdd(BusinessResultRecord record, AuditActor? actor = null)
    {
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

    public BusinessResultRecord? FindById(Guid id)
    {
        var row = db.SpKetQuaKinhDoanhBaiDoXe.AsNoTracking().SingleOrDefault(row => row.Id == id.ToString() && row.DaXoa == 0);
        return row is null ? null : Map(row);
    }

    public IReadOnlyList<BusinessResultRecord> List(IReadOnlySet<Guid>? parkingLotIds = null)
    {
        if (parkingLotIds is { Count: 0 }) return [];
        var records = db.SpKetQuaKinhDoanhBaiDoXe.AsNoTracking().Where(row => row.DaXoa == 0);
        if (parkingLotIds is not null)
        {
            string[] allowed = parkingLotIds.Select(id => id.ToString()).ToArray();
            records = records.Where(row => allowed.Contains(row.BaiDoXeId));
        }
        return records.AsEnumerable().Select(Map).ToArray();
    }

    public bool TryUpdate(Guid id, decimal revenue, int completedVehicleCount, string? note, AuditActor? actor = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(revenue);
        ArgumentOutOfRangeException.ThrowIfNegative(completedVehicleCount);
        string now = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
        return db.SpKetQuaKinhDoanhBaiDoXe.Where(row => row.Id == id.ToString() && row.DaXoa == 0)
            .ExecuteUpdate(setters => setters.SetProperty(row => row.DoanhThu, (double)revenue)
                .SetProperty(row => row.SoLuotXe, completedVehicleCount).SetProperty(row => row.GhiChu, note)
                .SetProperty(row => row.IdNguoiCapNhat, actor == null ? null : actor.Id)
                .SetProperty(row => row.NguoiCapNhat, actor == null ? null : actor.Name).SetProperty(row => row.NgayCapNhat, now)) != 0;
    }

    public bool TrySoftDelete(Guid id, AuditActor? actor = null)
    {
        string now = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);
        return db.SpKetQuaKinhDoanhBaiDoXe.Where(row => row.Id == id.ToString() && row.DaXoa == 0)
            .ExecuteUpdate(setters => setters.SetProperty(row => row.DaXoa, 1)
                .SetProperty(row => row.IdNguoiCapNhat, actor == null ? null : actor.Id)
                .SetProperty(row => row.NguoiCapNhat, actor == null ? null : actor.Name).SetProperty(row => row.NgayCapNhat, now)) != 0;
    }

    private static BusinessResultRecord Map(SpKetQuaKinhDoanhBaiDoXe row) => new(Guid.Parse(row.Id), Guid.Parse(row.BaiDoXeId),
        row.KyBaoCao, Convert.ToDecimal(row.DoanhThu), row.SoLuotXe, row.GhiChu);
}
