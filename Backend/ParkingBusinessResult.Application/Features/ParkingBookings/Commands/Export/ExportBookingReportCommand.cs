using ParkingBusinessResult.Application.Features.BusinessResults.Common;
using ParkingBusinessResult.Application.Features.BusinessReports.Common;

namespace ParkingBusinessResult.Application.Features.ParkingBookings.Commands.Export;

public sealed record ExportBookingReportCommand(DateTime FromInclusive, DateTime ToExclusive, string PeriodLabel,
    Guid? ParkingLotId, BusinessAccess Access, ReportActor Actor, string? District, IReadOnlySet<Guid>? ParkingLotIds = null,
    string FileFormat = "EXCEL", string ReportTitle = "Báo cáo từ lượt xe");
