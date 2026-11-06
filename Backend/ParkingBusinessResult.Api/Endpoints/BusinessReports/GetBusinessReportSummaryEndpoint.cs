using System.Security.Claims;
using ParkingBusinessResult.Api.Contracts;
using ParkingBusinessResult.Application.Features.BusinessReports.Queries.Summary;

namespace ParkingBusinessResult.Api.Endpoints.BusinessReports;

public static class GetBusinessReportSummaryEndpoint
{
    public static void Map(RouteGroupBuilder routes) => routes.MapGet("/summary", Handle);

    private static IResult Handle(string? reportingPeriod, Guid? parkingLotId, string? district, HttpRequest request,
        ClaimsPrincipal user, GetBusinessReportSummaryHandler handler)
    {
        if (string.IsNullOrWhiteSpace(reportingPeriod))
            return Results.Ok(ApiResponse<object>.BusinessError("Cần cung cấp kỳ báo cáo."));
        string[] allowedParameters = ["reportingPeriod", "parkingLotId", "district"];
        if (request.Query.Keys.Any(key => !allowedParameters.Contains(key, StringComparer.OrdinalIgnoreCase)))
            return Results.Ok(ApiResponse<object>.BusinessError("Bộ lọc báo cáo chưa được hỗ trợ."));

        BusinessReportSummary summary = handler.Handle(
            new GetBusinessReportSummaryQuery(reportingPeriod.Trim(), parkingLotId, EndpointSupport.GetAccess(user), district));
        return Results.Ok(ApiResponse<BusinessReportSummary>.Success(summary));
    }
}
