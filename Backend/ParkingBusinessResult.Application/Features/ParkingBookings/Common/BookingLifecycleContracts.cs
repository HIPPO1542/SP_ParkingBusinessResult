using ParkingBusinessResult.Application.Features.BusinessReports.Common;
using ParkingBusinessResult.Application.Features.BusinessResults.Common;
using ParkingBusinessResult.Domain;

namespace ParkingBusinessResult.Application.Features.ParkingBookings.Common;

public sealed record ReservationInput(Guid ParkingLotId, string LicensePlate, string VehicleType, DateTime ExpectedArrivalAt, DateTime ExpectedDepartureAt, string? Note);
public sealed record BookingEstimateInput(Guid ParkingLotId, string VehicleType, DateTime ExpectedArrivalAt, DateTime ExpectedDepartureAt);
public sealed record BookingEstimate(long BillableHours, decimal Amount, string DayType);
public sealed record BookingEstimateResult(BookingEstimate? Estimate, string? Error);
public sealed record BookingDetails(Guid Id, ReportParkingLot ParkingLot, string LicensePlate, string VehicleType,
    DateTime StartAt, DateTime? EndAt, string Status, decimal? Amount, long? BillableHours, string? Note,
    string? CreatedAt, string? CreatedBy, string? CreatedById, string? UpdatedAt, string? UpdatedBy, string? UpdatedById,
    decimal? EstimatedAmount);
public sealed record BookingWriteResult(BookingDetails? Booking, string? Error);
public sealed record CustomerBookingPage(IReadOnlyList<BookingDetails> Items, int TotalCount, int PageNumber, int PageSize);
public sealed record BookingOptions(IReadOnlyList<ReportParkingLot> ParkingLots, IReadOnlyList<string> VehicleTypes,
    CustomerBookingPage? MyBookings = null);

public interface IBookingLifecycleRepository
{
    BookingWriteResult Reserve(ReservationInput input, AuditActor actor, DateTime now);
    BookingWriteResult CheckIn(Guid id, BusinessAccess access, AuditActor actor, DateTime now);
    BookingWriteResult CheckOut(Guid id, string dayType, BusinessAccess access, AuditActor actor, DateTime now);
}

public interface ICustomerBookingQueries
{
    BookingOptions GetOptions();
    CustomerBookingPage GetMyBookings(string customerId, int pageNumber, int pageSize);
    BookingEstimateResult Estimate(BookingEstimateInput input);
}
