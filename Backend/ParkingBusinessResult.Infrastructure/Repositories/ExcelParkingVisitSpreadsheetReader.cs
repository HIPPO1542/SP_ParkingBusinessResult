using System.Globalization;
using ExcelDataReader;
using ParkingBusinessResult.Application.Features.ParkingVisits.Common;

namespace ParkingBusinessResult.Infrastructure.Repositories;

public sealed class ExcelParkingVisitSpreadsheetReader : IParkingVisitSpreadsheetReader
{
    private static readonly string[] Headers = ["Mã bãi", "Biển số", "Loại xe", "Giờ vào", "Giờ ra", "Số tiền đã thu (VND)", "Phương thức thanh toán"];

    static ExcelParkingVisitSpreadsheetReader() => System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

    public IReadOnlyList<ParkingVisitRow> Read(Stream content)
    {
        try { return ReadRows(content); }
        catch (ExcelDataReader.Exceptions.HeaderException exception)
        {
            throw new InvalidDataException("File Excel không hợp lệ.", exception);
        }
    }

    private static IReadOnlyList<ParkingVisitRow> ReadRows(Stream content)
    {
        using IExcelDataReader reader = ExcelReaderFactory.CreateOpenXmlReader(content);
        if (!reader.Read() || reader.FieldCount != Headers.Length ||
            Headers.Where((header, index) => !string.Equals(reader.GetValue(index)?.ToString()?.Trim(), header, StringComparison.OrdinalIgnoreCase)).Any())
            throw new InvalidDataException("File không đúng các cột của mẫu nhập lượt xe.");
        var rows = new List<ParkingVisitRow>();
        int rowNumber = 1;
        while (reader.Read())
        {
            rowNumber++;
            var cells = new string?[Headers.Length];
            for (int index = 0; index < cells.Length; index++)
            {
                object? value = reader.GetValue(index);
                cells[index] = value switch
                {
                    null => null,
                    DateTime date => date.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                    IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
                    _ => value.ToString()
                };
            }
            if (cells.All(string.IsNullOrWhiteSpace)) continue;
            if (rows.Count >= 50000) throw new InvalidDataException("File vượt quá 50.000 dòng dữ liệu.");
            rows.Add(new(rowNumber, cells));
        }
        return rows;
    }
}
