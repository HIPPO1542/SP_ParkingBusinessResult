using ParkingBusinessResult.Application.Features.ParkingBookings.Common;

namespace ParkingBusinessResult.Application.Features.ParkingBookings.Queries.List;

public sealed record ParkingBookingPage(IReadOnlyList<ParkingBookingItem> Items, int TotalCount, int PageNumber, int PageSize);

public sealed class ListParkingBookingsHandler(IParkingBookingListRepository repository)
{
    public ParkingBookingPage Handle(ListParkingBookingsQuery query)
    {
        ParkingBookingItem[] bookings = repository.List()
            .Where(booking => query.Access.CanAccess(booking.ParkingLotId))
            .Where(booking => query.ParkingLotId is null || booking.ParkingLotId == query.ParkingLotId)
            .Where(booking => query.Status is null || (query.Status == "PARKED" ? booking.ExitedAt is null : booking.ExitedAt is not null))
            .Where(booking => query.VehicleType is null || string.Equals(booking.VehicleType, query.VehicleType, StringComparison.OrdinalIgnoreCase))
            .Where(booking => MatchesDate(booking, query))
            .OrderByDescending(booking => booking.EnteredAt).ThenBy(booking => booking.Id).ToArray();
        long offset = ((long)query.PageNumber - 1) * query.PageSize;
        ParkingBookingItem[] page = offset >= bookings.Length ? [] : bookings.Skip((int)offset).Take(query.PageSize).ToArray();
        return new(page, bookings.Length, query.PageNumber, query.PageSize);
    }

    private static bool MatchesDate(ParkingBookingItem booking, ListParkingBookingsQuery query)
    {
        if (query.Day is null && query.Month is null && query.Year is null) return true;
        DateTime? date = query.DateField == "exitedAt" ? booking.ExitedAt : booking.EnteredAt;
        return date.HasValue && (query.Day is null || date.Value.Day == query.Day) &&
            (query.Month is null || date.Value.Month == query.Month) &&
            (query.Year is null || date.Value.Year == query.Year);
    }
}
