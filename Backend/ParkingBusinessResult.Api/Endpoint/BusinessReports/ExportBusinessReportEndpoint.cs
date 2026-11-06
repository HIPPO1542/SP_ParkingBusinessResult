using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;
using ParkingBusinessResult.Api.Contracts;
using ParkingBusinessResult.Application.Features.BusinessReports.Commands.Export;
using ParkingBusinessResult.Application.Features.BusinessReports.Common;

namespace ParkingBusinessResult.Api.Endpoint.BusinessReports;

public static class ExportBusinessReportEndpoint
{
    public sealed record Request(string? FileFormat, string? ReportingPeriod, Guid? ParkingLotId, string? ReportTitle, string? District)
    {
        [JsonExtensionData] public Dictionary<string, JsonElement>? ExtraFields { get; init; }
    }

    public static void Map(RouteGroupBuilder routes) => routes.MapPost("/export", Handle);

    private static IResult Handle(Request request, ClaimsPrincipal user, ExportBusinessReportHandler handler)
    {
        if (!user.IsInRole("ROLE_ADMIN") && !user.IsInRole("ROLE_DISTRICT_ADMIN"))
            return Results.Ok(ApiResponse<object>.BusinessError("Bạn không có quyền xuất báo cáo."));
        if (string.IsNullOrWhiteSpace(user.FindFirst("sub")?.Value))
            return Results.Ok(ApiResponse<object>.BusinessError("Không xác định được người xuất báo cáo."));
        if (request.FileFormat is not ("EXCEL" or "PDF"))
            return Results.Ok(ApiResponse<object>.BusinessError("Định dạng hỗ trợ: EXCEL hoặc PDF."));
        if (request.FileFormat == "PDF" && !OperatingSystem.IsWindows())
            return Results.Ok(ApiResponse<object>.BusinessError("Xuất PDF hiện chỉ hỗ trợ Windows."));
        if (string.IsNullOrWhiteSpace(request.ReportingPeriod) || request.ReportingPeriod.Length > 50 ||
            request.ReportTitle?.Length > 255 || request.ExtraFields is { Count: > 0 })
            return Results.Ok(ApiResponse<object>.BusinessError("Kỳ báo cáo không hợp lệ hoặc yêu cầu chứa trường chưa hỗ trợ."));

        string title = string.IsNullOrWhiteSpace(request.ReportTitle) ? "Báo cáo kết quả kinh doanh" : request.ReportTitle.Trim();
        string period = request.ReportingPeriod.Trim();
        try { XmlConvert.VerifyXmlChars(title); XmlConvert.VerifyXmlChars(period); }
        catch (XmlException) { return Results.Ok(ApiResponse<object>.BusinessError("Tiêu đề hoặc kỳ báo cáo chứa ký tự không hợp lệ.")); }

        SavedBusinessReport response = handler.Handle(new ExportBusinessReportCommand(period, request.ParkingLotId, title,
            EndpointSupport.GetAccess(user), request.FileFormat, EndpointSupport.GetReportActor(user), request.District));
        return Results.Ok(ApiResponse<SavedBusinessReport>.Success(response));
    }
}
