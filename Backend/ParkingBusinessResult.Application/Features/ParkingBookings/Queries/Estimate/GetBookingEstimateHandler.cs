using ParkingBusinessResult.Application.Features.ParkingBookings.Common;

namespace ParkingBusinessResult.Application.Features.ParkingBookings.Queries.Estimate;

public sealed record GetBookingEstimateQuery(BookingEstimateInput Input);

public sealed class GetBookingEstimateHandler(ICustomerBookingQueries repository, TimeProvider clock)
{
    public BookingEstimateResult Handle(GetBookingEstimateQuery query)
    {
        if (query.Input.ParkingLotId == Guid.Empty || string.IsNullOrWhiteSpace(query.Input.VehicleType) || query.Input.VehicleType.Length > 100 ||
            query.Input.ExpectedArrivalAt < clock.GetLocalNow().DateTime || query.Input.ExpectedDepartureAt <= query.Input.ExpectedArrivalAt)
            return new(null, "Bãi xe, loại xe hoặc khoảng thời gian dự kiến không hợp lệ.");
        return repository.Estimate(query.Input);
    }
}
