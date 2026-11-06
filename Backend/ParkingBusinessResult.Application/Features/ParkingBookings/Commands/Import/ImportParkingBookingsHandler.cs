using System.Globalization;
using ParkingBusinessResult.Application.Features.ParkingBookings.Common;

namespace ParkingBusinessResult.Application.Features.ParkingBookings.Commands.Import;

public sealed class ImportParkingBookingsHandler(IParkingBookingSpreadsheetReader reader, IParkingBookingImportRepository repository)
{
    private static readonly string[] DateFormats = ["yyyy-MM-dd HH:mm", "yyyy-MM-dd HH:mm:ss", "dd/MM/yyyy HH:mm", "dd/MM/yyyy HH:mm:ss"];

    public ParkingBookingImportResult Handle(ImportParkingBookingsCommand command, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<ParkingBookingRow> rows = reader.Read(command.Content, command.Extension);
        // A file is one import batch: row validation errors are reported, but a storage failure rolls back all valid rows.
        using IParkingBookingImportSession session = repository.BeginImport();
        var parkingLots = new Dictionary<string, Guid?>(StringComparer.OrdinalIgnoreCase);
        var errors = new List<ParkingBookingImportError>();
        var importedIds = new List<Guid>();
        var importedRows = new List<ParkingBookingImportedRow>();
        int success = 0;
        int duplicates = 0;
        foreach (ParkingBookingRow row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string?[] fields = row.Cells;
            string? parkingLotCode = string.IsNullOrWhiteSpace(fields[0]) ? null : fields[0]!.Trim();
            Guid? lotId = null;
            if (parkingLotCode is not null && !parkingLots.TryGetValue(parkingLotCode, out lotId))
            {
                lotId = session.FindAccessibleParkingLot(parkingLotCode, command.Access);
                parkingLots.Add(parkingLotCode, lotId);
            }
            string plate = fields[1]?.Trim().ToUpperInvariant() ?? "";
            string type = fields[2]?.Trim() ?? "";
            if (lotId is null || plate.Length is 0 or > 30 || type.Length is 0 or > 100 ||
                !TryDate(fields[3], out DateTime entered))
            {
                errors.Add(new(row.RowNumber, parkingLotCode, "Mã bãi không hợp lệ hoặc không có quyền; biển số, loại xe và giờ vào là bắt buộc."));
                continue;
            }
            DateTime? exited = null;
            if (!string.IsNullOrWhiteSpace(fields[4]))
            {
                if (!TryDate(fields[4], out DateTime parsed) || parsed <= entered)
                {
                    errors.Add(new(row.RowNumber, parkingLotCode, "Giờ ra không hợp lệ hoặc không sau giờ vào."));
                    continue;
                }
                exited = parsed;
            }
            decimal? amount = null;
            if (!string.IsNullOrWhiteSpace(fields[5]))
            {
                if (!decimal.TryParse(fields[5], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal parsedAmount) || parsedAmount < 0)
                {
                    errors.Add(new(row.RowNumber, parkingLotCode, "Số tiền thực thu phải là số không âm."));
                    continue;
                }
                amount = parsedAmount;
            }
            ParkingBookingSaved? saved = session.TryInsert(new ParkingBookingInput(lotId.Value, plate, type, entered, exited, amount), command.UserId, command.Username);
            if (saved is null)
            {
                duplicates++;
                errors.Add(new(row.RowNumber, parkingLotCode, $"Lượt xe {plate} tại bãi {parkingLotCode} với giờ vào này đã tồn tại."));
                continue;
            }
            importedIds.Add(saved.Id);
            importedRows.Add(new(row.RowNumber, parkingLotCode!, saved));
            success++;
        }
        cancellationToken.ThrowIfCancellationRequested();
        session.Commit();
        return new(rows.Count, success, errors.Count, importedIds, errors, duplicates, importedRows);
    }

    private static bool TryDate(string? text, out DateTime result) => DateTime.TryParseExact(text?.Trim(), DateFormats,
        CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
}
