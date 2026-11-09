using System.Globalization;
using Microsoft.EntityFrameworkCore;
using ParkingBusinessResult.Infrastructure.Persistence;
using ParkingBusinessResult.Application.Features.BusinessReports.Common;
using ParkingBusinessResult.Application.Features.BusinessReports.Queries.List;
namespace ParkingBusinessResult.Infrastructure.Repositories;
public sealed class ReportMetadataReader(ParkingDbContext db) : IReportMetadataReader
{
    public IReadOnlyDictionary<Guid, BusinessReportMetadata> FindMany(IReadOnlySet<Guid> resultIds)
    {
        string[] ids = resultIds.Select(id => id.ToString()).ToArray();
        var rows = from row in db.SpKetQuaKinhDoanhBaiDoXe.AsNoTracking()
                   join lot in db.DlKthtBaiDoXe on row.BaiDoXeId equals lot.Id
                   join unit in db.SpDonViKinhDoanhQuanLy on lot.DonViQuanLyId equals unit.Id into units
                   from unit in units.DefaultIfEmpty()
                   where row.DaXoa == 0 && ids.Contains(row.Id)
                   select new { row.Id, LotId = lot.Id, lot.Ma, lot.Ten, lot.DiaChi, Operator = unit == null ? null : unit.TenDonVi,
                       row.NgayTao, row.NguoiTao, row.IdNguoiTao, row.NgayCapNhat, row.NguoiCapNhat, row.IdNguoiCapNhat };
        return rows.AsEnumerable().ToDictionary(row => Guid.Parse(row.Id), row => new BusinessReportMetadata(
            new ReportParkingLot(Guid.Parse(row.LotId), row.Ma, row.Ten, row.DiaChi), row.Operator,
            Date(row.NgayTao), row.NguoiTao, row.IdNguoiTao, Date(row.NgayCapNhat), row.NguoiCapNhat, row.IdNguoiCapNhat));
    }
    private static string? Date(string? text) => DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
        ? date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : null;
}
