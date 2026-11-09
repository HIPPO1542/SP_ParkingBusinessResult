using ParkingBusinessResult.Application.Features.BusinessReports.Common;

namespace ParkingBusinessResult.Infrastructure.Reports;

public sealed class ReportDocumentWriter : IReportDocumentWriter
{
    public ReportDocument Write(string format, string title, string period, IReadOnlyList<ReportRow> rows,
        IReadOnlyList<string>? vehicleTypes = null) => format switch
    {
        "EXCEL" => new(ReportExcelWriter.Write(title, period, rows, vehicleTypes), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", ".xlsx"),
        "PDF" => new(ReportPdfWriter.Write(title, period, rows, vehicleTypes), "application/pdf", ".pdf"),
        _ => throw new ArgumentException("Unsupported report format.", nameof(format))
    };
}

internal static class ReportColumns
{
    public static string[] Headers(IReadOnlyList<string>? vehicleTypes) => vehicleTypes is null
        ? ["Mã bãi đỗ xe", "Tên bãi đỗ xe", "Kỳ báo cáo", "Doanh thu (VND)", "Lượt xe hoàn tất"]
        : ["Mã bãi đỗ xe", "Tên bãi đỗ xe", "Kỳ báo cáo", "Doanh thu (VND)", "Lượt xe hoàn tất", "Lượt xe chưa hoàn tất",
            .. vehicleTypes.Select(type => $"Lượt {type}")];

    public static int VehicleCount(ReportRow row, string type) =>
        row.VehicleCounts is not null && row.VehicleCounts.TryGetValue(type, out int count) ? count : 0;
}
