using ParkingBusinessResult.Application.Features.BusinessResults.Common;

namespace ParkingBusinessResult.Application.Features.ParkingBookings.Common;

public sealed record BookingRow(int RowNumber, string?[] Cells);
public sealed record BookingInput(Guid ParkingLotId, string LicensePlate, string VehicleType,
    DateTime EnteredAt, DateTime? ExitedAt, decimal? CollectedAmount);
public sealed record BookingImportError(int RowNumber, string? ParkingLotCode, string ErrorMessage);
public sealed record BookingSaved(Guid Id, string LicensePlate, string VehicleType, DateTime EnteredAt,
    DateTime? ExitedAt, decimal? CollectedAmount);
public sealed record BookingImportedRow(int RowNumber, string ParkingLotCode, BookingSaved Booking);
public sealed record BookingImportResult(int TotalRows, int SuccessRows, int ErrorRows,
    IReadOnlyList<Guid> ImportedIds, IReadOnlyList<BookingImportError> Errors,
    int DuplicateRows, IReadOnlyList<BookingImportedRow> ImportedRows);

public interface IBookingSpreadsheetReader
{
    IReadOnlyList<BookingRow> Read(Stream content, string extension);
}

public interface IBookingImportRepository
{
    IBookingImportSession BeginImport();
}

public interface IBookingImportSession : IDisposable
{
    Guid? FindAccessibleParkingLot(string code, BusinessAccess access);
    BookingSaved? TryInsert(BookingInput booking, string? userId, string? username);
    void Commit();
}
