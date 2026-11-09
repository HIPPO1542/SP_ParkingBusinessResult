using ParkingBusinessResult.Application.Features.BusinessResults.Common;
using ParkingBusinessResult.Application.Features.ParkingBookings.Queries.List;

namespace ParkingBusinessResult.Application.Features.ParkingBookings.Common;

public sealed record BookingItem(Guid Id, Guid ParkingLotId, string LicensePlate, string VehicleType,
    DateTime EnteredAt, DateTime? ExitedAt, decimal? CollectedAmount,
    string? ParkingLotCode, string? ParkingLotName, string Status);

public sealed record BookingOverview(decimal CollectedRevenue, long CompletedVehicleCount,
    long ParkedVehicleCount, long ReservedBookingCount, long MissingAmountCount);

public interface IBookingListRepository
{
    BookingPage GetPage(ListBookingsQuery query);
    IReadOnlyList<string> GetVehicleTypes(BusinessAccess access);
    BookingOverview GetOverview(BusinessAccess access);
}
