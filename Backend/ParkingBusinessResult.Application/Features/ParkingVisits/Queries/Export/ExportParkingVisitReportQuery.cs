using ParkingBusinessResult.Application.Features.BusinessResults.Common;

namespace ParkingBusinessResult.Application.Features.ParkingVisits.Queries.Export;

public sealed record ExportParkingVisitReportQuery(DateTime FromInclusive, DateTime ToExclusive, string PeriodLabel,
    Guid? ParkingLotId, BusinessAccess Access, string? District, IReadOnlySet<Guid>? ParkingLotIds = null);
