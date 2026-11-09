using Microsoft.EntityFrameworkCore;
using ParkingBusinessResult.Domain;
using ParkingBusinessResult.Infrastructure.Persistence;
using ParkingBusinessResult.Application.Features.BusinessReports.Common;
using ParkingBusinessResult.Application.Features.BusinessReports.Queries.List;

namespace ParkingBusinessResult.Infrastructure.Repositories;

public sealed class ReportQueryRepository(ParkingDbContext db) : IReportQueryRepository
{
    public IReadOnlyList<BusinessResultRecord> List(ListReportsQuery query)
    {
        var records = db.SpKetQuaKinhDoanhBaiDoXe.AsNoTracking().Where(row => row.DaXoa == 0);
        if (!query.Access.IsAdministrator)
        {
            string[] allowed = query.Access.ParkingLotIds.Select(id => id.ToString()).ToArray();
            records = records.Where(row => allowed.Contains(row.BaiDoXeId));
        }
        if (query.ReportingPeriod is not null) records = records.Where(row => row.KyBaoCao == query.ReportingPeriod);
        if (query.ParkingLotId.HasValue)
        {
            string lotId = query.ParkingLotId.Value.ToString();
            records = records.Where(row => row.BaiDoXeId == lotId);
        }
        string? keyword = string.IsNullOrWhiteSpace(query.Keyword) ? null : query.Keyword.Trim();
        string? district = string.IsNullOrWhiteSpace(query.District) ? null : query.District.Trim();
        if (keyword is not null || district is not null)
        {
            var matchingLots = db.DlKthtBaiDoXe.AsNoTracking().AsQueryable();
            if (keyword is not null) matchingLots = matchingLots.Where(lot => ParkingDbContext.ContainsIgnoreCase(lot.Ten, keyword) || ParkingDbContext.ContainsIgnoreCase(lot.DiaChi, keyword));
            if (district is not null) matchingLots = matchingLots.Where(lot => ParkingDbContext.ContainsIgnoreCase(lot.DiaChi, district));
            records = records.Where(row => matchingLots.Select(lot => lot.Id).Contains(row.BaiDoXeId));
        }
        var ordered = records.OrderByDescending(row => row.DoanhThu).ThenBy(row => row.BaiDoXeId).AsQueryable();
        if (query.PageSize.HasValue)
        {
            long offset = ((long)(query.PageNumber ?? 1) - 1) * query.PageSize.Value;
            if (offset > int.MaxValue) return [];
            ordered = ordered.Skip((int)offset).Take(query.PageSize.Value);
        }
        return ordered.AsEnumerable().Select(row => new BusinessResultRecord(Guid.Parse(row.Id), Guid.Parse(row.BaiDoXeId),
            row.KyBaoCao, Convert.ToDecimal(row.DoanhThu), row.SoLuotXe, row.GhiChu)).ToArray();
    }
}
