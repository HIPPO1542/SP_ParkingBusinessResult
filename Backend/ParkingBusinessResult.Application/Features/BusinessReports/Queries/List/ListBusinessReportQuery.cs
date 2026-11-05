using ParkingBusinessResult.Application.Features.BusinessResults.Common;

namespace ParkingBusinessResult.Application.Features.BusinessReports.Queries.List;

public sealed record ListBusinessReportQuery(string? ReportingPeriod, Guid? ParkingLotId, BusinessAccess Access, string? Keyword = null, string? District = null);
