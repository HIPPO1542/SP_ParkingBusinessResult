using ParkingBusinessResult.Application.Features.BusinessResults.Common;

namespace ParkingBusinessResult.Application.Features.ParkingBookings.Common;

public sealed record CompletedParkingBooking(Guid ParkingLotId, decimal? CollectedAmount,
    string? ParkingLotCode, string? ParkingLotName, string? ParkingLotAddress);

public sealed record ParkingBookingSummaryFilter(DateTime FromInclusive, DateTime ToExclusive, BusinessAccess Access,
    Guid? ParkingLotId = null, IReadOnlySet<Guid>? ParkingLotIds = null, string? District = null);

public interface IParkingBookingSummaryRepository
{
    IReadOnlyList<CompletedParkingBooking> ListCompleted(ParkingBookingSummaryFilter filter);
}
