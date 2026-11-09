using ParkingBusinessResult.Application.Features.BusinessResults.Common;

namespace ParkingBusinessResult.Application.Features.ParkingBookings.Common;

public sealed record CompletedBooking(Guid ParkingLotId, decimal? CollectedAmount,
    string? ParkingLotCode, string? ParkingLotName, string? ParkingLotAddress, string VehicleType);

public sealed record BookingSummaryFilter(DateTime FromInclusive, DateTime ToExclusive, BusinessAccess Access,
    Guid? ParkingLotId = null, IReadOnlySet<Guid>? ParkingLotIds = null, string? District = null, string? VehicleType = null);

public interface IBookingSummaryRepository
{
    IReadOnlyList<CompletedBooking> ListCompleted(BookingSummaryFilter filter);
}
