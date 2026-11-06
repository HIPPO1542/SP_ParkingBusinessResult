namespace ParkingBusinessResult.Application.Features.ParkingBookings.Common;

public sealed record ParkingBookingItem(Guid Id, Guid ParkingLotId, string LicensePlate, string VehicleType,
    DateTime EnteredAt, DateTime? ExitedAt, decimal? CollectedAmount,
    string? ParkingLotCode, string? ParkingLotName);

public interface IParkingBookingListRepository
{
    IReadOnlyList<ParkingBookingItem> List();
}
