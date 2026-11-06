using ParkingBusinessResult.Application.Features.BusinessReports.Common;
using ParkingBusinessResult.Application.Features.ParkingBookings.Common;

namespace ParkingBusinessResult.Application.Features.ParkingBookings.Queries.Export;

public sealed record ParkingBookingExportResult(IReadOnlyList<BusinessReportRow> Rows, int MissingAmountCount);

public sealed class ExportParkingBookingReportHandler(IParkingBookingSummaryRepository repository)
{
    public ParkingBookingExportResult Handle(ExportParkingBookingReportQuery query)
    {
        CompletedParkingBooking[] bookings = repository.ListCompleted(query.FromInclusive, query.ToExclusive)
            .Where(booking => query.Access.CanAccess(booking.ParkingLotId))
            .Where(booking => query.ParkingLotId is null || booking.ParkingLotId == query.ParkingLotId)
            .Where(booking => query.ParkingLotIds is null || query.ParkingLotIds.Contains(booking.ParkingLotId))
            .Where(booking => string.IsNullOrWhiteSpace(query.District) || (booking.ParkingLotAddress?.Contains(query.District.Trim(), StringComparison.OrdinalIgnoreCase) ?? false)).ToArray();
        int missing = bookings.Count(booking => booking.CollectedAmount is null);
        BusinessReportRow[] rows = bookings.GroupBy(booking => booking.ParkingLotId).OrderBy(group => group.Key)
            .Select(group => new BusinessReportRow(group.Key, query.PeriodLabel,
                group.Where(booking => booking.CollectedAmount.HasValue).Sum(booking => booking.CollectedAmount!.Value), group.Count(),
                group.First().ParkingLotCode, group.First().ParkingLotName)).ToArray();
        return new(rows, missing);
    }
}
