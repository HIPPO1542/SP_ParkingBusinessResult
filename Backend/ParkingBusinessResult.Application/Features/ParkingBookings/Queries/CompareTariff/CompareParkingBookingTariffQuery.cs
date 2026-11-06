using ParkingBusinessResult.Application.Features.BusinessResults.Common;

namespace ParkingBusinessResult.Application.Features.ParkingBookings.Queries.CompareTariff;

public sealed record CompareParkingBookingTariffQuery(Guid BookingId, string DayType, BusinessAccess Access);
