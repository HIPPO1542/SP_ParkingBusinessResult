using ParkingBusinessResult.Application.Features.BusinessResults.Common;

namespace ParkingBusinessResult.Application.Features.ParkingBookings.Queries.Export;

public sealed record ExportParkingBookingReportQuery(DateTime FromInclusive, DateTime ToExclusive, string PeriodLabel,
    Guid? ParkingLotId, BusinessAccess Access, string? District, IReadOnlySet<Guid>? ParkingLotIds = null);
