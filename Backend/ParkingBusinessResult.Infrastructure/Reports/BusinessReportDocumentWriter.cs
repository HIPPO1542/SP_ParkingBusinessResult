using ParkingBusinessResult.Application.Features.BusinessReports.Common;

namespace ParkingBusinessResult.Infrastructure.Reports;

public sealed class BusinessReportDocumentWriter : IBusinessReportDocumentWriter
{
    public ReportDocument Write(string format, string title, string period, IReadOnlyList<BusinessReportRow> rows) => format switch
    {
        "EXCEL" => new(BusinessReportExcelWriter.Write(title, period, rows), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", ".xlsx"),
        "PDF" => new(BusinessReportPdfWriter.Write(title, period, rows), "application/pdf", ".pdf"),
        _ => throw new ArgumentException("Unsupported report format.", nameof(format))
    };
}
