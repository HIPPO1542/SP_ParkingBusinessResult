using ParkingBusinessResult.Application.Features.BusinessResults.Common;

namespace ParkingBusinessResult.Application.Features.ParkingBookings.Common;

public sealed record ParkingTariffRule(decimal InitialHours, decimal InitialPrice, decimal AdditionalHourPrice);
public sealed record BookingTariffData(DateTime EnteredAt, DateTime? ExitedAt, decimal? CollectedAmount,
    IReadOnlyList<ParkingTariffRule> Rules);

public interface IBookingTariffRepository
{
    BookingTariffData? Find(Guid bookingId, string dayType, BusinessAccess access);
}
