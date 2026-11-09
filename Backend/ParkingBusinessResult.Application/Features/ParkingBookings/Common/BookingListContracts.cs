using ParkingBusinessResult.Application.Features.BusinessResults.Common;
using ParkingBusinessResult.Application.Features.ParkingBookings.Queries.List;

namespace ParkingBusinessResult.Application.Features.ParkingBookings.Common;

public sealed record BookingItem(Guid Id, Guid ParkingLotId, string LicensePlate, string VehicleType,
    DateTime EnteredAt, DateTime? ExitedAt, decimal? CollectedAmount,
    string? ParkingLotCode, string? ParkingLotName, string Status);

public interface IBookingListRepository
{
    BookingPage GetPage(ListBookingsQuery query);
    IReadOnlyList<string> GetVehicleTypes(BusinessAccess access);
}
