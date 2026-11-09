using ParkingBusinessResult.Application.Features.ParkingBookings.Common;

namespace ParkingBusinessResult.Application.Features.ParkingBookings.Queries.MyBookings;

public sealed record GetMyBookingsQuery(string CustomerId, int PageNumber, int PageSize);

public sealed class GetMyBookingsHandler(ICustomerBookingQueries repository)
{
    public CustomerBookingPage Handle(GetMyBookingsQuery query) =>
        repository.GetMyBookings(query.CustomerId, query.PageNumber, query.PageSize);
}
