namespace ParkingBusinessResult.Application.Features.ParkingBookings.Common;

public sealed record CompletedParkingBooking(Guid ParkingLotId, decimal? CollectedAmount,
    string? ParkingLotCode, string? ParkingLotName, string? ParkingLotAddress);

public interface IParkingBookingSummaryRepository
{
    IReadOnlyList<CompletedParkingBooking> ListCompleted(DateTime fromInclusive, DateTime toExclusive);
}
