using System.Security.Claims;
using ParkingBusinessResult.Api.Contracts;
using ParkingBusinessResult.Application.Features.BusinessReports;
using ParkingBusinessResult.Application.Features.BusinessReports.Queries.Summary;

namespace ParkingBusinessResult.Api.Endpoint.BusinessReports;

public static class GetBusinessReportSummaryEndpoint
{
    public static void Map(RouteGroupBuilder routes) => routes.MapGet("/summary", Handle);

    private static IResult Handle(string? reportingPeriod, Guid? parkingLotId, ClaimsPrincipal user, GetBusinessReportSummaryHandler handler)
    {
        if (string.IsNullOrWhiteSpace(reportingPeriod))
            return Results.Ok(ApiResponse<object>.BusinessError("Cần cung cấp kỳ báo cáo."));

        BusinessSummary summary = handler.Handle(
            new GetBusinessReportSummaryQuery(reportingPeriod.Trim(), parkingLotId, EndpointSupport.GetAccess(user)));
        return Results.Ok(ApiResponse<BusinessSummary>.Success(summary));
    }
}
