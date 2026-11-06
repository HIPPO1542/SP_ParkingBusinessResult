using System.Security.Claims;
using ParkingBusinessResult.Api.Contracts;
using ParkingBusinessResult.Application.Features.BusinessReports.Queries.Download;

namespace ParkingBusinessResult.Api.Endpoints.BusinessReports;

public static class DownloadReportEndpoint
{
    public static void Map(WebApplication app) => app.MapGet("/static/exports/{fileName}", Handle)
        .RequireAuthorization("BusinessResultAccess");

    private static IResult Handle(string fileName, ClaimsPrincipal user, DownloadReportHandler handler)
    {
        var document = handler.Handle(new DownloadReportQuery(EndpointSupport.GetReportActor(user), fileName));
        if (document is null)
            return Results.Ok(ApiResponse<object>.BusinessError("Không tìm thấy báo cáo hoặc bạn không có quyền tải file này."));
        return Results.File(document.Path, document.ContentType, document.FileName);
    }
}
