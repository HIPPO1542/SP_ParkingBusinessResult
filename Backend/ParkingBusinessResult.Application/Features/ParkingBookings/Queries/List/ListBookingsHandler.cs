using ParkingBusinessResult.Application.Features.ParkingBookings.Common;

namespace ParkingBusinessResult.Application.Features.ParkingBookings.Queries.List;

public sealed record BookingPage(IReadOnlyList<BookingItem> Items, int TotalCount, int PageNumber, int PageSize, BookingOverview Overview);

public sealed class ListBookingsHandler(IBookingListRepository repository)
{
    public BookingPage Handle(ListBookingsQuery query) => repository.GetPage(query);
}
