using ParkingBusinessResult.Application.Features.BusinessReports.Common;
using ParkingBusinessResult.Application.Features.ParkingBookings.Common;

namespace ParkingBusinessResult.Application.Features.ParkingBookings.Commands.Export;

public sealed record BookingExportResult(SavedBusinessReport? Report, int MissingAmountCount);

public sealed class ExportBookingReportHandler(IBookingSummaryRepository repository, IReportDocumentWriter writer,
    IReportExportStore store)
{
    public BookingExportResult Handle(ExportBookingReportCommand command)
    {
        IReadOnlyList<CompletedBooking> bookings = repository.ListCompleted(new BookingSummaryFilter(
            command.FromInclusive, command.ToExclusive, command.Access, command.ParkingLotId, command.ParkingLotIds, command.District));
        int missing = bookings.Count(booking => booking.CollectedAmount is null);
        if (missing > 0) return new(null, missing);
        ReportRow[] rows = bookings.GroupBy(booking => booking.ParkingLotId).OrderBy(group => group.Key)
            .Select(group => new ReportRow(group.Key, command.PeriodLabel,
                group.Where(booking => booking.CollectedAmount.HasValue).Sum(booking => booking.CollectedAmount!.Value), group.Count(),
                group.First().ParkingLotCode, group.First().ParkingLotName)).ToArray();
        ReportDocument document = writer.Write(command.FileFormat, command.ReportTitle, command.PeriodLabel, rows);
        SavedBusinessReport report = store.Save(command.Actor, command.FileFormat, document, rows.Length);
        return new(report, 0);
    }
}
