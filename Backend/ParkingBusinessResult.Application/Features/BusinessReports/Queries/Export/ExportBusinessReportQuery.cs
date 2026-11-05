using ParkingBusinessResult.Application.Features.BusinessResults.Common;

namespace ParkingBusinessResult.Application.Features.BusinessReports.Queries.Export;

public sealed record ExportBusinessReportQuery(string ReportingPeriod, Guid? ParkingLotId, string? ReportTitle, BusinessAccess Access);
