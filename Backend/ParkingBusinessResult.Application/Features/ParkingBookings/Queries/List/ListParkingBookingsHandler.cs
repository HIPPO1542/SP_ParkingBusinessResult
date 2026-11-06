using ParkingBusinessResult.Application.Features.ParkingBookings.Common;

namespace ParkingBusinessResult.Application.Features.ParkingBookings.Queries.List;

public sealed record ParkingBookingPage(IReadOnlyList<ParkingBookingItem> Items, int TotalCount, int PageNumber, int PageSize);

public sealed class ListParkingBookingsHandler(IParkingBookingListRepository repository)
{
    public ParkingBookingPage Handle(ListParkingBookingsQuery query) => repository.GetPage(query);
}
