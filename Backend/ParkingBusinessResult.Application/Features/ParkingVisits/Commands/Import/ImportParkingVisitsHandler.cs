using System.Globalization;
using ParkingBusinessResult.Application.Features.ParkingVisits.Common;

namespace ParkingBusinessResult.Application.Features.ParkingVisits.Commands.Import;

public sealed class ImportParkingVisitsHandler(IParkingVisitSpreadsheetReader reader, IParkingVisitImportRepository repository)
{
    private static readonly string[] DateFormats = ["yyyy-MM-dd HH:mm", "yyyy-MM-dd HH:mm:ss", "dd/MM/yyyy HH:mm", "dd/MM/yyyy HH:mm:ss"];

    public ParkingVisitImportResult Handle(ImportParkingVisitsCommand command)
    {
        IReadOnlyList<ParkingVisitRow> rows = reader.Read(command.Content, command.Extension);
        var errors = new List<ParkingVisitImportError>();
        var importedIds = new List<Guid>();
        var importedRows = new List<ParkingVisitImportedRow>();
        int success = 0;
        int duplicates = 0;
        foreach (ParkingVisitRow row in rows)
        {
            string?[] fields = row.Cells;
            string? parkingLotCode = string.IsNullOrWhiteSpace(fields[0]) ? null : fields[0]!.Trim();
            Guid? lotId = parkingLotCode is null ? null : repository.FindAccessibleParkingLot(parkingLotCode, command.Access);
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
            string? paymentMethod = string.IsNullOrWhiteSpace(fields[6]) ? null : fields[6]!.Trim();
            if (paymentMethod?.Length > 100)
            {
                errors.Add(new(row.RowNumber, parkingLotCode, "Phương thức thanh toán quá dài."));
                continue;
            }
            ParkingVisitSaved saved = repository.Upsert(new ParkingVisitInput(lotId.Value, plate, type, entered, exited, amount, paymentMethod));
            importedIds.Add(saved.Id);
            importedRows.Add(new(row.RowNumber, parkingLotCode!, saved));
            if (saved.IsDuplicate) duplicates++;
            success++;
        }
        return new(rows.Count, success, errors.Count, importedIds, errors, duplicates, importedRows);
    }

    private static bool TryDate(string? text, out DateTime result) => DateTime.TryParseExact(text?.Trim(), DateFormats,
        CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
}
