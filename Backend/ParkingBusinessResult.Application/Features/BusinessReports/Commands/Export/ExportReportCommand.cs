using ParkingBusinessResult.Application.Features.BusinessResults.Common;
using ParkingBusinessResult.Application.Features.BusinessReports.Common;

namespace ParkingBusinessResult.Application.Features.BusinessReports.Commands.Export;

public sealed record ExportReportCommand(string ReportingPeriod, Guid? ParkingLotId, string ReportTitle,
    BusinessAccess Access, string FileFormat, ReportActor Actor, string? District = null);
