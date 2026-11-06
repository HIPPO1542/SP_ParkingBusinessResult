using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;
using ParkingBusinessResult.Api.Contracts;
using ParkingBusinessResult.Application.Features.ParkingVisits.Queries.Export;
using ParkingBusinessResult.Infrastructure.Reports;

namespace ParkingBusinessResult.Api.Endpoint.ParkingVisits;

public static class ExportParkingVisitReportEndpoint
{
    public sealed record Request(string? FileFormat, string? ReportingPeriod, string? FromDate, string? ToDate,
        Guid? ParkingLotId, string? ReportTitle, string? District, Guid[]? ParkingLotIds)
    {
        [JsonExtensionData] public Dictionary<string, JsonElement>? ExtraFields { get; init; }
    }

    public static void Map(RouteGroupBuilder routes) => routes.MapPost("/export", Handle);

    private static IResult Handle(Request request, ClaimsPrincipal user, ExportParkingVisitReportHandler handler)
    {
        if (!user.IsInRole("ROLE_ADMIN") && !user.IsInRole("ROLE_DISTRICT_ADMIN"))
            return Results.Ok(ApiResponse<object>.BusinessError("Bạn không có quyền xuất báo cáo."));
        ParkingVisitPeriod? period = ParkingVisitPeriodParser.Parse(request.ReportingPeriod, request.FromDate, request.ToDate);
        if (request.FileFormat is not ("EXCEL" or "PDF") || request.ExtraFields is { Count: > 0 } || request.ReportTitle?.Length > 255 || period is null || request.ParkingLotIds?.Any(id => id == Guid.Empty) == true)
            return Results.Ok(ApiResponse<object>.BusinessError("Định dạng, kỳ báo cáo hoặc dữ liệu yêu cầu không hợp lệ."));
        if (request.FileFormat == "PDF" && !OperatingSystem.IsWindows())
            return Results.Ok(ApiResponse<object>.BusinessError("Xuất PDF thử nghiệm hiện chỉ hỗ trợ Windows."));
        string title = string.IsNullOrWhiteSpace(request.ReportTitle) ? "Báo cáo từ lượt xe" : request.ReportTitle.Trim();
        try { XmlConvert.VerifyXmlChars(title); }
        catch (XmlException) { return Results.Ok(ApiResponse<object>.BusinessError("Tiêu đề chứa ký tự không hợp lệ.")); }
        IReadOnlySet<Guid>? parkingLotIds = request.ParkingLotIds is { Length: > 0 } ? request.ParkingLotIds.ToHashSet() : null;
        ParkingVisitExportResult result = handler.Handle(new ExportParkingVisitReportQuery(period.From, period.ToExclusive, period.Label, request.ParkingLotId, EndpointSupport.GetAccess(user), request.District, parkingLotIds));
        if (result.MissingAmountCount > 0)
            return Results.Ok(ApiResponse<object>.BusinessError($"Có {result.MissingAmountCount} lượt hoàn tất chưa có số tiền thực thu."));
        if (request.FileFormat == "PDF")
            return Results.File(BusinessReportPdfWriter.Write(title, period.Label, result.Rows), "application/pdf", "bao-cao-luot-xe.pdf");
        return Results.File(BusinessReportExcelWriter.Write(title, period.Label, result.Rows),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "bao-cao-luot-xe.xlsx");
    }
}
