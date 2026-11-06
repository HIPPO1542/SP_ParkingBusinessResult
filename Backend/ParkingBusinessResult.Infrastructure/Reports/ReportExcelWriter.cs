using System.Globalization;
using System.IO.Compression;
using System.Xml.Linq;
using ParkingBusinessResult.Application.Features.BusinessReports.Common;
using ParkingBusinessResult.Domain;

namespace ParkingBusinessResult.Infrastructure.Reports;

public static class ReportExcelWriter
{
    private static readonly XNamespace Main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace Relationships = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

    public static byte[] Write(string title, string period, IReadOnlyList<ReportRow> records)
    {
        var rows = new List<XElement>
        {
            Row(Text(title)),
            Row(Text("Kỳ báo cáo"), Text(period)),
            Row(Text("Mã bãi đỗ xe"), Text("Tên bãi đỗ xe"), Text("Kỳ báo cáo"), Text("Doanh thu (VND)"), Text("Lượt xe hoàn tất"), Text("Bình quân mỗi lượt (VND)"))
        };
        foreach (ReportRow record in records)
        {
            rows.Add(Row(Text(record.ParkingLotCode ?? ""), Text(record.ParkingLotName ?? ""), Text(record.ReportingPeriod), Number(record.Revenue),
                Number(record.CompletedVehicleCount), Number(RevenueMetrics.AverageRevenuePerVehicle(record.Revenue, record.CompletedVehicleCount))));
        }

        BusinessSummary summary = BusinessSummaryCalculator.Calculate(records.Select(record =>
            new BusinessSummaryEntry(record.Revenue, record.CompletedVehicleCount)));
        rows.Add(Row(Text("Tổng"), Text(""), Text(period), Number(summary.TotalRevenue), Number(summary.TotalVehicleCount), Number(summary.AverageRevenuePerVehicle)));
        var sheet = new XDocument(new XElement(Main + "worksheet",
            new XElement(Main + "cols", new XElement(Main + "col", new XAttribute("min", 1), new XAttribute("max", 1), new XAttribute("width", 40), new XAttribute("customWidth", 1)),
                new XElement(Main + "col", new XAttribute("min", 2), new XAttribute("max", 6), new XAttribute("width", 27), new XAttribute("customWidth", 1))),
            new XElement(Main + "sheetData", rows)));

        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            Add(archive, "[Content_Types].xml", XDocument.Parse("""
                <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/><Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/></Types>
                """));
            Add(archive, "_rels/.rels", XDocument.Parse("""
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>
                """));
            Add(archive, "xl/workbook.xml", new XDocument(new XElement(Main + "workbook", new XAttribute(XNamespace.Xmlns + "r", Relationships),
                new XElement(Main + "sheets", new XElement(Main + "sheet", new XAttribute("name", "Báo cáo"), new XAttribute("sheetId", 1), new XAttribute(Relationships + "id", "rId1"))))));
            Add(archive, "xl/_rels/workbook.xml.rels", XDocument.Parse("""
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/></Relationships>
                """));
            Add(archive, "xl/worksheets/sheet1.xml", sheet);
        }
        return stream.ToArray();
    }

    private static XElement Row(params XElement[] cells) => new(Main + "row", cells);
    private static XElement Text(string value) => new(Main + "c", new XAttribute("t", "inlineStr"), new XElement(Main + "is", new XElement(Main + "t", value)));
    private static XElement Number(decimal value) => new(Main + "c", new XElement(Main + "v", value.ToString(CultureInfo.InvariantCulture)));
    private static void Add(ZipArchive archive, string path, XDocument document)
    {
        using Stream entry = archive.CreateEntry(path).Open();
        document.Save(entry);
    }
}
