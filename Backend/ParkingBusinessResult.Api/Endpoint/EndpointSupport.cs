using System.Security.Claims;
using ParkingBusinessResult.Api.Contracts;
using ParkingBusinessResult.Application.Features.BusinessResults.Common;
using ParkingBusinessResult.Domain;

namespace ParkingBusinessResult.Api.Endpoint;

internal static class EndpointSupport
{
    public static BusinessAccess GetAccess(ClaimsPrincipal user)
    {
        var parkingLotIds = new HashSet<Guid>();
        foreach (Claim claim in user.FindAll("parking_lot_id"))
        {
            if (Guid.TryParse(claim.Value, out Guid parkingLotId)) parkingLotIds.Add(parkingLotId);
        }

        return new BusinessAccess(user.IsInRole("ROLE_ADMIN"), parkingLotIds);
    }

    public static IResult Respond(BusinessResultOutcome<ParkingBusinessResultRecord> outcome) =>
        outcome.Error is null
            ? Results.Ok(ApiResponse<ParkingBusinessResultRecord>.Success(outcome.Data!))
            : Results.Ok(ApiResponse<ParkingBusinessResultRecord>.BusinessError(outcome.Error));

    public static IResult InvalidBody() =>
        Results.Ok(ApiResponse<object>.BusinessError("Dữ liệu yêu cầu thiếu trường bắt buộc hoặc chứa trường không hỗ trợ."));
}
