using ParkingBusinessResult.Application.Features.BusinessReports.Common;
using ParkingBusinessResult.Application.Features.ParkingBookings.Common;

namespace ParkingBusinessResult.Application.Features.ParkingBookings.Commands.Export;

public sealed record ParkingBookingExportResult(SavedBusinessReport? Report, int MissingAmountCount);

public sealed class ExportParkingBookingReportHandler(IParkingBookingSummaryRepository repository, IBusinessReportDocumentWriter writer,
    IBusinessReportExportStore store)
{
    public ParkingBookingExportResult Handle(ExportParkingBookingReportCommand command)
    {
        IReadOnlyList<CompletedParkingBooking> bookings = repository.ListCompleted(new ParkingBookingSummaryFilter(
            command.FromInclusive, command.ToExclusive, command.Access, command.ParkingLotId, command.ParkingLotIds, command.District));
        int missing = bookings.Count(booking => booking.CollectedAmount is null);
        if (missing > 0) return new(null, missing);
        BusinessReportRow[] rows = bookings.GroupBy(booking => booking.ParkingLotId).OrderBy(group => group.Key)
            .Select(group => new BusinessReportRow(group.Key, command.PeriodLabel,
                group.Where(booking => booking.CollectedAmount.HasValue).Sum(booking => booking.CollectedAmount!.Value), group.Count(),
                group.First().ParkingLotCode, group.First().ParkingLotName)).ToArray();
        ReportDocument document = writer.Write(command.FileFormat, command.ReportTitle, command.PeriodLabel, rows);
        SavedBusinessReport report = store.Save(command.Actor, command.FileFormat, document, rows.Length);
        return new(report, 0);
    }
}
