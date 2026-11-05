using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;
using ParkingBusinessResult.Api.Contracts;
using ParkingBusinessResult.Application.Features.BusinessReports.Queries.Export;
using ParkingBusinessResult.Application.Features.BusinessReports.Common;
using ParkingBusinessResult.Domain;
using ParkingBusinessResult.Infrastructure.Reports;

namespace ParkingBusinessResult.Api.Endpoint.BusinessReports;

public static class ExportBusinessReportEndpoint
{
    public sealed record Response(Guid Id, string FileUrl, string FileName, string FileFormat, long FileSize,
        int RecordCount, string CreatedAt, string? CreatedBy, string CreatedById,
        string UpdatedAt, string? UpdatedBy, string UpdatedById);
    public sealed record Request(string? FileFormat, string? ReportingPeriod, Guid? ParkingLotId, string? ReportTitle, string? District)
    {
        [JsonExtensionData] public Dictionary<string, JsonElement>? ExtraFields { get; init; }
    }

    public static void Map(RouteGroupBuilder routes) => routes.MapPost("/export", Handle);

    private static IResult Handle(Request request, ClaimsPrincipal user, ExportBusinessReportHandler handler, BusinessReportExportStore store,
        IBusinessReportParkingLotSearch parkingLotSearch)
    {
        if (!user.IsInRole("ROLE_ADMIN") && !user.IsInRole("ROLE_DISTRICT_ADMIN"))
            return Results.Ok(ApiResponse<object>.BusinessError("Bạn không có quyền xuất báo cáo."));
        if (!Guid.TryParse(user.FindFirst("sub")?.Value, out Guid userId))
            return Results.Ok(ApiResponse<object>.BusinessError("Không xác định được người xuất báo cáo."));
        if (request.FileFormat is not ("EXCEL" or "PDF"))
            return Results.Ok(ApiResponse<object>.BusinessError("Định dạng hỗ trợ: EXCEL hoặc PDF."));
        if (request.FileFormat == "PDF" && !OperatingSystem.IsWindows())
            return Results.Ok(ApiResponse<object>.BusinessError("Xuất PDF thử nghiệm hiện chỉ hỗ trợ Windows."));
        if (string.IsNullOrWhiteSpace(request.ReportingPeriod) || request.ReportingPeriod.Length > 50 ||
            request.ReportTitle?.Length > 255 || request.ExtraFields is { Count: > 0 })
            return Results.Ok(ApiResponse<object>.BusinessError("Kỳ báo cáo không hợp lệ hoặc yêu cầu chứa trường chưa hỗ trợ."));

        string title = string.IsNullOrWhiteSpace(request.ReportTitle) ? "Báo cáo kết quả kinh doanh" : request.ReportTitle.Trim();
        string period = request.ReportingPeriod.Trim();
        try { XmlConvert.VerifyXmlChars(title); XmlConvert.VerifyXmlChars(period); }
        catch (XmlException) { return Results.Ok(ApiResponse<object>.BusinessError("Tiêu đề hoặc kỳ báo cáo chứa ký tự không hợp lệ.")); }

        IReadOnlyList<ParkingBusinessResultRecord> records = handler.Handle(
            new ExportBusinessReportQuery(period, request.ParkingLotId, title, EndpointSupport.GetAccess(user), request.District));
        IReadOnlyDictionary<Guid, ReportParkingLot> lots = parkingLotSearch.GetParkingLots(records.Select(record => record.ParkingLotId).ToHashSet());
        BusinessReportRow[] rows = records.Select(record => new BusinessReportRow(record.ParkingLotId, record.ReportingPeriod,
            record.Revenue, record.CompletedVehicleCount, lots[record.ParkingLotId].Code, lots[record.ParkingLotId].Name)).ToArray();
        byte[] content = request.FileFormat == "PDF"
            ? BusinessReportPdfWriter.Write(title, period, rows)
            : BusinessReportExcelWriter.Write(title, period, rows);
        Guid id = Guid.NewGuid();
        DateTime now = DateTime.Now;
        string extension = request.FileFormat == "PDF" ? ".pdf" : ".xlsx";
        string fileName = $"BaoCao_KetQuaKinhDoanh_{now:yyyyMMdd_HHmmss}_{id:N}{extension}";
        string? username = store.FindUsername(userId);
        store.Save(userId, fileName, content);
        string date = now.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        var response = new Response(id, $"/static/exports/{fileName}", fileName, request.FileFormat, content.LongLength,
            records.Count, date, username, userId.ToString(), date, username, userId.ToString());
        return Results.Ok(ApiResponse<Response>.Success(response));
    }
}
