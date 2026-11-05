using System.Security.Claims;
using ParkingBusinessResult.Api.Contracts;
using ParkingBusinessResult.Application.Features.BusinessReports.Queries.List;
using ParkingBusinessResult.Domain;

namespace ParkingBusinessResult.Api.Endpoint.BusinessReports;

public static class ListBusinessReportEndpoint
{
    public static void Map(RouteGroupBuilder routes) => routes.MapGet("/", Handle);

    private static IResult Handle(string? reportingPeriod, Guid? parkingLotId, ClaimsPrincipal user, ListBusinessReportHandler handler)
    {
        if (string.IsNullOrWhiteSpace(reportingPeriod))
            return Results.Ok(ApiResponse<object>.BusinessError("Cần cung cấp kỳ báo cáo."));

        IReadOnlyList<ParkingBusinessResultRecord> records = handler.Handle(
            new ListBusinessReportQuery(reportingPeriod.Trim(), parkingLotId, EndpointSupport.GetAccess(user)));
        return Results.Ok(ApiResponse<IReadOnlyList<ParkingBusinessResultRecord>>.Success(records));
    }
}
