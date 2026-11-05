using ParkingBusinessResult.Application.Features.BusinessResults.Common;

namespace ParkingBusinessResult.Application.Features.ParkingVisits.Common;

public sealed record ParkingVisitRow(int RowNumber, string?[] Cells);
public sealed record ParkingVisitInput(Guid ParkingLotId, string LicensePlate, string VehicleType,
    DateTime EnteredAt, DateTime? ExitedAt, decimal? CollectedAmount, string? PaymentMethod);
public sealed record ParkingVisitImportError(int RowNumber, string? ParkingLotCode, string ErrorMessage);
public sealed record ParkingVisitSaved(Guid Id, string LicensePlate, string VehicleType, DateTime EnteredAt,
    DateTime? ExitedAt, decimal? CollectedAmount, string? PaymentMethod);
public sealed record ParkingVisitImportedRow(int RowNumber, string ParkingLotCode, ParkingVisitSaved Visit);
public sealed record ParkingVisitImportResult(int TotalRows, int SuccessRows, int ErrorRows,
    IReadOnlyList<Guid> ImportedIds, IReadOnlyList<ParkingVisitImportError> Errors,
    int DuplicateRows, IReadOnlyList<ParkingVisitImportedRow> ImportedRows);

public interface IParkingVisitSpreadsheetReader
{
    IReadOnlyList<ParkingVisitRow> Read(Stream content, string extension);
}

public interface IParkingVisitImportRepository
{
    Guid? FindAccessibleParkingLot(string code, BusinessAccess access);
    ParkingVisitSaved? TryInsert(ParkingVisitInput visit);
}
