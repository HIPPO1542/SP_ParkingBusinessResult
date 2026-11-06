using Microsoft.EntityFrameworkCore;
using ParkingBusinessResult.Infrastructure.Data;
using ParkingBusinessResult.Application.Features.BusinessReports.Common;
namespace ParkingBusinessResult.Infrastructure.Repositories;
public sealed class SqliteBusinessReportParkingLotSearch(string databasePath) : IBusinessReportParkingLotSearch
{
    public IReadOnlyDictionary<Guid, ReportParkingLot> GetParkingLots(IReadOnlySet<Guid> ids)
    {
        using var db = ParkingContextFactory.Create(databasePath);
        string[] selected = ids.Select(id => id.ToString()).ToArray();
        return db.DlKthtBaiDoXe.AsNoTracking().Where(lot => selected.Contains(lot.Id))
            .Select(lot => new { lot.Id, lot.Ma, lot.Ten, lot.DiaChi }).AsEnumerable()
            .ToDictionary(lot => Guid.Parse(lot.Id), lot => new ReportParkingLot(Guid.Parse(lot.Id), lot.Ma, lot.Ten, lot.DiaChi));
    }
    public IReadOnlySet<Guid> FindMatchingLots(string? keyword, string? district)
    {
        using var db = ParkingContextFactory.Create(databasePath);
        var lots = db.DlKthtBaiDoXe.AsNoTracking().AsQueryable();
        if (keyword is not null) lots = lots.Where(lot => ParkingDbContext.ContainsIgnoreCase(lot.Ten, keyword) || ParkingDbContext.ContainsIgnoreCase(lot.DiaChi, keyword));
        if (district is not null) lots = lots.Where(lot => ParkingDbContext.ContainsIgnoreCase(lot.DiaChi, district));
        return lots.Select(lot => lot.Id).AsEnumerable().Select(Guid.Parse).ToHashSet();
    }
}
