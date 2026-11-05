using System.Globalization;
using ParkingBusinessResult.Application.Features.BusinessReports.Common;
using ParkingBusinessResult.Domain;
using PdfSharp.Drawing;
using PdfSharp.Drawing.Layout;
using PdfSharp.Fonts;
using PdfSharp.Pdf;

namespace ParkingBusinessResult.Infrastructure.Reports;

public static class BusinessReportPdfWriter
{
    public static byte[] Write(string title, string period, IReadOnlyList<BusinessReportRow> records)
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Xuất PDF thử nghiệm cần phông chữ Windows.");

        GlobalFontSettings.UseWindowsFontsUnderWindows = true;
        using var document = new PdfDocument();
        var normal = new XFont("Arial", 9);
        var bold = new XFont("Arial", 9, XFontStyleEx.Bold);
        var heading = new XFont("Arial", 12, XFontStyleEx.Bold);
        XGraphics? graphics = null;
        double y = 0;

        void NewPage()
        {
            graphics?.Dispose();
            PdfPage page = document.AddPage();
            page.Orientation = PdfSharp.PageOrientation.Landscape;
            graphics = XGraphics.FromPdfPage(page);
            new XTextFormatter(graphics).DrawString(title, heading, XBrushes.Black, new XRect(35, 24, page.Width.Point - 70, 50));
            graphics.DrawString($"Kỳ báo cáo: {period}", normal, XBrushes.Black, new XPoint(35, 88));
            y = 111;
            DrawRow(graphics, bold, y, "Mã bãi đỗ xe", "Kỳ báo cáo", "Doanh thu (VND)", "Lượt xe", "Bình quân (VND)");
            y += 22;
        }

        NewPage();
        foreach (BusinessReportRow record in records)
        {
            if (y > 550) NewPage();
            DrawRow(graphics!, normal, y, record.ParkingLotId.ToString(), record.ReportingPeriod,
                Format(record.Revenue), record.CompletedVehicleCount.ToString(CultureInfo.InvariantCulture),
                Format(RevenueMetrics.AverageRevenuePerVehicle(record.Revenue, record.CompletedVehicleCount)));
            y += 20;
        }

        if (y > 550) NewPage();
        BusinessSummary summary = BusinessSummaryCalculator.Calculate(records.Select(record =>
            new BusinessSummaryEntry(record.Revenue, record.CompletedVehicleCount)));
        DrawRow(graphics!, bold, y, "Tổng", period, Format(summary.TotalRevenue),
            summary.TotalVehicleCount.ToString(CultureInfo.InvariantCulture), Format(summary.AverageRevenuePerVehicle));
        graphics!.Dispose();
        using var output = new MemoryStream();
        document.Save(output, false);
        return output.ToArray();
    }

    private static string Format(decimal amount) => amount.ToString("N2", CultureInfo.InvariantCulture);

    private static void DrawRow(XGraphics graphics, XFont font, double y, string lot, string period, string revenue, string vehicles, string average)
    {
        graphics.DrawString(lot, font, XBrushes.Black, new XRect(35, y, 220, 18), XStringFormats.CenterLeft);
        graphics.DrawString(period, font, XBrushes.Black, new XRect(260, y, 105, 18), XStringFormats.CenterLeft);
        graphics.DrawString(revenue, font, XBrushes.Black, new XRect(365, y, 135, 18), XStringFormats.CenterRight);
        graphics.DrawString(vehicles, font, XBrushes.Black, new XRect(515, y, 80, 18), XStringFormats.CenterRight);
        graphics.DrawString(average, font, XBrushes.Black, new XRect(610, y, 175, 18), XStringFormats.CenterRight);
    }
}
