using ParkingBusinessResult.Application.Features.BusinessReports.Common;

namespace ParkingBusinessResult.Infrastructure.Reports;

public sealed class ReportDocumentWriter : IReportDocumentWriter
{
    public ReportDocument Write(string format, string title, string period, IReadOnlyList<ReportRow> rows) => format switch
    {
        "EXCEL" => new(ReportExcelWriter.Write(title, period, rows), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", ".xlsx"),
        "PDF" => new(ReportPdfWriter.Write(title, period, rows), "application/pdf", ".pdf"),
        _ => throw new ArgumentException("Unsupported report format.", nameof(format))
    };
}
