using ParkingBusinessResult.Application.Features.BusinessResults.Common;

namespace ParkingBusinessResult.Application.Features.ParkingBookings.Common;

public sealed record ParkingTariffRule(decimal InitialHours, decimal InitialPrice, decimal AdditionalHourPrice);
public sealed record ParkingBookingTariffData(DateTime EnteredAt, DateTime? ExitedAt, decimal? CollectedAmount,
    IReadOnlyList<ParkingTariffRule> Rules);

public interface IParkingBookingTariffRepository
{
    ParkingBookingTariffData? Find(Guid bookingId, string dayType, BusinessAccess access);
}
