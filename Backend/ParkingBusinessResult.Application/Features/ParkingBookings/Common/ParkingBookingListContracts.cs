using ParkingBusinessResult.Application.Features.BusinessResults.Common;
using ParkingBusinessResult.Application.Features.ParkingBookings.Queries.List;

namespace ParkingBusinessResult.Application.Features.ParkingBookings.Common;

public sealed record ParkingBookingItem(Guid Id, Guid ParkingLotId, string LicensePlate, string VehicleType,
    DateTime EnteredAt, DateTime? ExitedAt, decimal? CollectedAmount,
    string? ParkingLotCode, string? ParkingLotName);

public interface IParkingBookingListRepository
{
    ParkingBookingPage GetPage(ListParkingBookingsQuery query);
    IReadOnlyList<string> GetVehicleTypes(BusinessAccess access);
}
