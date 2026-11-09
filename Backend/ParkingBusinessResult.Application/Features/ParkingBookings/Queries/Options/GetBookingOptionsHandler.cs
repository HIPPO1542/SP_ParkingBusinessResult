using ParkingBusinessResult.Application.Features.ParkingBookings.Common;

namespace ParkingBusinessResult.Application.Features.ParkingBookings.Queries.Options;

public sealed record GetBookingOptionsQuery;

public sealed class GetBookingOptionsHandler(ICustomerBookingQueries repository)
{
    public BookingOptions Handle(GetBookingOptionsQuery query) => repository.GetOptions();
}
