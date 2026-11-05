using ParkingBusinessResult.Application.Features.BusinessResults.Common;

namespace ParkingBusinessResult.Application.Features.BusinessReports.Queries.Summary;

public sealed record GetBusinessReportSummaryQuery(string ReportingPeriod, Guid? ParkingLotId, BusinessAccess Access);
