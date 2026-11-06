using ParkingBusinessResult.Application.Features.BusinessResults.Common;

namespace ParkingBusinessResult.Application.Features.ParkingBookings.Queries.Summary;

public sealed record GetParkingBookingSummaryQuery(DateTime FromInclusive, DateTime ToExclusive, string PeriodLabel,
    Guid? ParkingLotId, BusinessAccess Access, bool IsMonthly, string? District, IReadOnlySet<Guid>? ParkingLotIds = null);
