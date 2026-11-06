using ParkingBusinessResult.Application.Features.BusinessReports.Common;
using ParkingBusinessResult.Application.Features.ParkingBookings.Common;

namespace ParkingBusinessResult.Application.Features.ParkingBookings.Queries.Export;

public sealed record ParkingBookingExportResult(ReportDocument? Document, int MissingAmountCount);

public sealed class ExportParkingBookingReportHandler(IParkingBookingSummaryRepository repository, IBusinessReportDocumentWriter writer)
{
    public ParkingBookingExportResult Handle(ExportParkingBookingReportQuery query)
    {
        IReadOnlyList<CompletedParkingBooking> bookings = repository.ListCompleted(new ParkingBookingSummaryFilter(
            query.FromInclusive, query.ToExclusive, query.Access, query.ParkingLotId, query.ParkingLotIds, query.District));
        int missing = bookings.Count(booking => booking.CollectedAmount is null);
        if (missing > 0) return new(null, missing);
        BusinessReportRow[] rows = bookings.GroupBy(booking => booking.ParkingLotId).OrderBy(group => group.Key)
            .Select(group => new BusinessReportRow(group.Key, query.PeriodLabel,
                group.Where(booking => booking.CollectedAmount.HasValue).Sum(booking => booking.CollectedAmount!.Value), group.Count(),
                group.First().ParkingLotCode, group.First().ParkingLotName)).ToArray();
        return new(writer.Write(query.FileFormat, query.ReportTitle, query.PeriodLabel, rows), 0);
    }
}
