using System.Security.Claims;
using ParkingBusinessResult.Api.Contracts;
using ParkingBusinessResult.Application.Features.BusinessResults.Common;
using ParkingBusinessResult.Domain;
using ParkingBusinessResult.Application.Features.BusinessReports.Common;

namespace ParkingBusinessResult.Api.Endpoints;

internal static class EndpointSupport
{
    public static AuditActor GetAuditActor(ClaimsPrincipal user)
    {
        ReportActor actor = GetReportActor(user);
        return new(actor.Id, actor.Name);
    }

    public static ReportActor GetReportActor(ClaimsPrincipal user) => new(
        user.FindFirst("sub")?.Value ?? throw new InvalidOperationException("Missing authenticated subject."),
        user.FindFirst("preferred_username")?.Value ?? user.FindFirst("unique_name")?.Value ?? user.FindFirst("name")?.Value,
        user.FindFirst("iss")?.Value);
    public static BusinessAccess GetAccess(ClaimsPrincipal user)
    {
        var parkingLotIds = new HashSet<Guid>();
        foreach (Claim claim in user.FindAll("parking_lot_id"))
        {
            if (Guid.TryParse(claim.Value, out Guid parkingLotId)) parkingLotIds.Add(parkingLotId);
        }

        return new BusinessAccess(user.IsInRole("ROLE_ADMIN"), parkingLotIds);
    }

    public static IResult Respond(BusinessResultOutcome<BusinessResultRecord> outcome) =>
        outcome.Error is null
            ? Results.Ok(ApiResponse<BusinessResultRecord>.Success(outcome.Data!))
            : Results.Ok(ApiResponse<BusinessResultRecord>.BusinessError(outcome.Error));

    public static IResult InvalidBody() =>
        Results.Ok(ApiResponse<object>.BusinessError("Dữ liệu yêu cầu thiếu trường bắt buộc hoặc chứa trường không hỗ trợ."));
}
