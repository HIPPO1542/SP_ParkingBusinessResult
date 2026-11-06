using ParkingBusinessResult.Application.Features.BusinessResults.Common;

namespace ParkingBusinessResult.Application.Features.ParkingBookings.Common;

public sealed record ParkingBookingRow(int RowNumber, string?[] Cells);
public sealed record ParkingBookingInput(Guid ParkingLotId, string LicensePlate, string VehicleType,
    DateTime EnteredAt, DateTime? ExitedAt, decimal? CollectedAmount);
public sealed record ParkingBookingImportError(int RowNumber, string? ParkingLotCode, string ErrorMessage);
public sealed record ParkingBookingSaved(Guid Id, string LicensePlate, string VehicleType, DateTime EnteredAt,
    DateTime? ExitedAt, decimal? CollectedAmount);
public sealed record ParkingBookingImportedRow(int RowNumber, string ParkingLotCode, ParkingBookingSaved Booking);
public sealed record ParkingBookingImportResult(int TotalRows, int SuccessRows, int ErrorRows,
    IReadOnlyList<Guid> ImportedIds, IReadOnlyList<ParkingBookingImportError> Errors,
    int DuplicateRows, IReadOnlyList<ParkingBookingImportedRow> ImportedRows);

public interface IParkingBookingSpreadsheetReader
{
    IReadOnlyList<ParkingBookingRow> Read(Stream content, string extension);
}

public interface IParkingBookingImportRepository
{
    Guid? FindAccessibleParkingLot(string code, BusinessAccess access);
    ParkingBookingSaved? TryInsert(ParkingBookingInput booking);
}
