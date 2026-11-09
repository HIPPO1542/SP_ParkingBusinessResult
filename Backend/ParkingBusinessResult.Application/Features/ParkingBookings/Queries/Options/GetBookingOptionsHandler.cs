using ParkingBusinessResult.Application.Features.ParkingBookings.Common;

namespace ParkingBusinessResult.Application.Features.ParkingBookings.Queries.Options;

public sealed record GetBookingOptionsQuery(string? CustomerId = null);

public sealed class GetBookingOptionsHandler(ICustomerBookingQueries repository)
{
    public BookingOptions Handle(GetBookingOptionsQuery query)
    {
        BookingOptions options = repository.GetOptions();
        return query.CustomerId is null ? options : options with { MyBookings = repository.GetMyBookings(query.CustomerId, 1, 10) };
    }
}
