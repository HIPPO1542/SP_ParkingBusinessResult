using ParkingBusinessResult.Application.Features.BusinessResults.Common;

namespace ParkingBusinessResult.Application.Features.BusinessReports.Queries.Summary;

public sealed record GetReportSummaryQuery(string ReportingPeriod, Guid? ParkingLotId, BusinessAccess Access, string? District = null);
