using ParkingBusinessResult.Application.Features.BusinessResults.Common;

namespace ParkingBusinessResult.Application.Features.ParkingBookings.Common;

public sealed record CompletedBooking(Guid ParkingLotId, decimal? CollectedAmount,
    string? ParkingLotCode, string? ParkingLotName, string? ParkingLotAddress, string VehicleType);
public sealed record UnfinishedBookingCount(Guid ParkingLotId, string VehicleType, int Count,
    string? ParkingLotCode, string? ParkingLotName);

public sealed record BookingExportData(IReadOnlyList<CompletedBooking> Completed, IReadOnlyList<UnfinishedBookingCount> Unfinished);
public sealed record BookingOpenCounts(long ParkedVehicleCount, long ReservedBookingCount);
public sealed record BookingSummaryData(IReadOnlyList<CompletedBooking> Completed, BookingOpenCounts OpenCounts,
    IReadOnlyList<CompletedBooking> Previous);

public sealed record BookingSummaryFilter(DateTime FromInclusive, DateTime ToExclusive, BusinessAccess Access,
    Guid? ParkingLotId = null, IReadOnlySet<Guid>? ParkingLotIds = null, string? District = null, string? VehicleType = null);

public interface IBookingSummaryRepository
{
    IReadOnlyList<CompletedBooking> ListCompleted(BookingSummaryFilter filter);
    BookingExportData ReadForExport(BookingSummaryFilter filter);
    BookingOpenCounts GetOpenCounts(BookingSummaryFilter filter);
    BookingSummaryData ReadSummary(BookingSummaryFilter filter, BookingSummaryFilter? previousPeriodFilter);
}
