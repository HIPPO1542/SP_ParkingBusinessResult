using System.Globalization;
using ParkingBusinessResult.Application.Features.BusinessReports.Common;
using PdfSharp.Drawing;
using PdfSharp.Drawing.Layout;
using PdfSharp.Fonts;
using PdfSharp.Pdf;

namespace ParkingBusinessResult.Infrastructure.Reports;

public static class ReportPdfWriter
{
    public static byte[] Write(string title, string period, IReadOnlyList<ReportRow> records, IReadOnlyList<string>? vehicleTypes = null)
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Xuất PDF cần phông chữ Windows.");
        GlobalFontSettings.UseWindowsFontsUnderWindows = true;
        using var document = new PdfDocument();
        var normal = new XFont("Arial", 9);
        var bold = new XFont("Arial", 9, XFontStyleEx.Bold);
        var heading = new XFont("Arial", 12, XFontStyleEx.Bold);
        IReadOnlyList<string>?[] groups = vehicleTypes is null ? [null] :
            vehicleTypes.Chunk(3).Cast<IReadOnlyList<string>?>().DefaultIfEmpty(Array.Empty<string>()).ToArray();
        for (int groupIndex = 0; groupIndex < groups.Length; groupIndex++)
        {
            IReadOnlyList<string>? types = groups[groupIndex];
            string[] headers = ReportColumns.Headers(types);
            XGraphics? graphics = null;
            double[] widths = [];
            double y = 0;
            double bottom = 0;
            void NewPage()
            {
                graphics?.Dispose();
                PdfPage page = document.AddPage();
                page.Orientation = PdfSharp.PageOrientation.Landscape;
                graphics = XGraphics.FromPdfPage(page);
                var columns = types is null ? new List<double> { 75, 150, 105, 100, 80 } : new List<double> { 75, 150, 105, 100, 80, 80 };
                double remaining = page.Width.Point - 70 - columns.Sum();
                if (types is { Count: > 0 }) columns.AddRange(Enumerable.Repeat(remaining / types.Count, types.Count));
                else columns[1] += remaining;
                widths = columns.ToArray();
                new XTextFormatter(graphics).DrawString(title, heading, XBrushes.Black, new XRect(35, 24, page.Width.Point - 70, 50));
                graphics.DrawString($"Kỳ báo cáo: {period}", normal, XBrushes.Black, new XPoint(35, 88));
                if (groups.Length > 1) graphics.DrawString($"Nhóm cột loại xe {groupIndex + 1}/{groups.Length}", normal, XBrushes.Black,
                    new XRect(page.Width.Point - 240, 75, 205, 18), XStringFormats.CenterRight);
                y = 111;
                y += DrawRow(graphics, bold, y, headers, widths, true);
                bottom = page.Height.Point - 35;
            }
            NewPage();
            foreach (ReportRow record in records)
            {
                var values = new List<string>
                {
                    record.ParkingLotCode ?? "", record.ParkingLotName ?? "", record.ReportingPeriod,
                    Format(record.Revenue), record.CompletedVehicleCount.ToString(CultureInfo.InvariantCulture)
                };
                if (types is not null)
                {
                    values.Add((record.UnfinishedVehicleCount ?? 0).ToString(CultureInfo.InvariantCulture));
                    values.AddRange(types.Select(type => ReportColumns.VehicleCount(record, type).ToString(CultureInfo.InvariantCulture)));
                }
                string[] cells = values.ToArray();
                if (y + RowHeight(graphics!, normal, cells, widths) > bottom) NewPage();
                y += DrawRow(graphics!, normal, y, cells, widths);
            }
            var totals = new List<string>
            {
                "Tổng", "", period, Format(records.Sum(record => record.Revenue)),
                records.Sum(record => (long)record.CompletedVehicleCount).ToString(CultureInfo.InvariantCulture)
            };
            if (types is not null)
            {
                totals.Add(records.Sum(record => (long)(record.UnfinishedVehicleCount ?? 0)).ToString(CultureInfo.InvariantCulture));
                totals.AddRange(types.Select(type => records.Sum(record => (long)ReportColumns.VehicleCount(record, type)).ToString(CultureInfo.InvariantCulture)));
            }
            string[] totalCells = totals.ToArray();
            if (y + RowHeight(graphics!, bold, totalCells, widths) > bottom) NewPage();
            DrawRow(graphics!, bold, y, totalCells, widths);
            graphics!.Dispose();
        }
        using var output = new MemoryStream();
        document.Save(output, false);
        return output.ToArray();
    }

    private static string Format(decimal amount) => amount.ToString("N2", CultureInfo.InvariantCulture);

    private static string[] Wrap(XGraphics graphics, XFont font, string text, double width)
    {
        var lines = new List<string>();
        foreach (string paragraph in text.Replace("\r", "").Split('\n'))
        {
            string line = "";
            foreach (string word in paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                string candidate = line.Length == 0 ? word : line + " " + word;
                if (graphics.MeasureString(candidate, font).Width <= width) { line = candidate; continue; }
                if (line.Length > 0) { lines.Add(line); line = ""; }
                foreach (char letter in word)
                {
                    if (line.Length > 0 && graphics.MeasureString(line + letter, font).Width > width) { lines.Add(line); line = ""; }
                    line += letter;
                }
            }
            lines.Add(line);
        }
        return lines.ToArray();
    }

    private static double RowHeight(XGraphics graphics, XFont font, string[] values, double[] widths) =>
        Math.Max(22, values.Select((value, index) => Wrap(graphics, font, value, widths[index] - 8).Length).Max() * 12 + 6);

    private static double DrawRow(XGraphics graphics, XFont font, double y, string[] values, double[] widths, bool header = false)
    {
        double height = RowHeight(graphics, font, values, widths);
        double x = 35;
        for (int index = 0; index < values.Length; index++)
        {
            if (header) graphics.DrawRectangle(XBrushes.WhiteSmoke, x, y, widths[index], height);
            graphics.DrawRectangle(XPens.LightGray, x, y, widths[index], height);
            string[] lines = Wrap(graphics, font, values[index], widths[index] - 8);
            for (int line = 0; line < lines.Length; line++)
                graphics.DrawString(lines[line], font, XBrushes.Black, new XRect(x + 4, y + 3 + line * 12, widths[index] - 8, 12),
                    !header && index >= 3 ? XStringFormats.CenterRight : XStringFormats.CenterLeft);
            x += widths[index];
        }
        return height;
    }
}
