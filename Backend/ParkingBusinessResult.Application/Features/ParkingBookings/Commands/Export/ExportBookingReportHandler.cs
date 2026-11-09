using ParkingBusinessResult.Application.Features.BusinessReports.Common;
using ParkingBusinessResult.Application.Features.ParkingBookings.Common;

namespace ParkingBusinessResult.Application.Features.ParkingBookings.Commands.Export;

public sealed record BookingExportResult(SavedBusinessReport? Report, int MissingAmountCount);

public sealed class ExportBookingReportHandler(IBookingSummaryRepository repository, IReportDocumentWriter writer,
    IReportExportStore store)
{
    public BookingExportResult Handle(ExportBookingReportCommand command)
    {
        var filter = new BookingSummaryFilter(command.FromInclusive, command.ToExclusive, command.Access,
            command.ParkingLotId, command.ParkingLotIds, command.District, command.VehicleType);
        BookingExportData exportData = repository.ReadForExport(filter);
        IReadOnlyList<CompletedBooking> bookings = exportData.Completed;
        int missing = bookings.Count(booking => booking.CollectedAmount is null);
        if (missing > 0) return new(null, missing);
        IReadOnlyList<UnfinishedBookingCount> unfinished = exportData.Unfinished;
        var completedByLot = bookings.ToLookup(booking => booking.ParkingLotId);
        var unfinishedByLot = unfinished.ToLookup(booking => booking.ParkingLotId);
        Guid[] lotIds = bookings.Select(booking => booking.ParkingLotId).Concat(unfinished.Select(booking => booking.ParkingLotId)).Distinct().Order().ToArray();
        ReportRow[] rows = lotIds.Select(id =>
        {
            var completed = completedByLot[id];
            CompletedBooking? first = completed.FirstOrDefault();
            UnfinishedBookingCount? firstUnfinished = unfinishedByLot[id].FirstOrDefault();
            var counts = completed.GroupBy(booking => booking.VehicleType, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);
            return new ReportRow(id, command.PeriodLabel, completed.Sum(booking => booking.CollectedAmount!.Value), completed.Count(),
                first?.ParkingLotCode ?? firstUnfinished?.ParkingLotCode, first?.ParkingLotName ?? firstUnfinished?.ParkingLotName,
                unfinishedByLot[id].Sum(booking => booking.Count), counts);
        }).ToArray();
        string[] vehicleTypes = bookings.Select(booking => booking.VehicleType).Concat(unfinished.Select(booking => booking.VehicleType))
            .Concat(command.VehicleType is null ? [] : new[] { command.VehicleType })
            .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(type => type, StringComparer.OrdinalIgnoreCase).ToArray();
        ReportDocument document = writer.Write(command.FileFormat, command.ReportTitle, command.PeriodLabel, rows, vehicleTypes);
        SavedBusinessReport report = store.Save(command.Actor, command.FileFormat, document, rows.Length);
        return new(report, 0);
    }
}
