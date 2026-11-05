using ParkingBusinessResult.Api.Contracts;
using ParkingBusinessResult.Domain;
using System.Security.Claims;

namespace ParkingBusinessResult.Api.Development;

public static class BusinessResultEndpoints
{
    public static void Map(WebApplication app)
    {
        RouteGroupBuilder routes = app.MapGroup("/dev/business-results").RequireAuthorization("BusinessResultAccess");

        routes.MapGet("/", (ClaimsPrincipal user, IBusinessResultRepository repository) =>
            Results.Ok(ApiResponse<IReadOnlyList<ParkingBusinessResultRecord>>.Success(
                repository.List().Where(result => CanAccess(user, result.ParkingLotId)).ToArray())));

        routes.MapGet("/{id:guid}", (Guid id, ClaimsPrincipal user, IBusinessResultRepository repository) =>
        {
            ParkingBusinessResultRecord? businessResult = repository.FindById(id);
            return businessResult is null || !CanAccess(user, businessResult.ParkingLotId) ? NotFound() : Success(businessResult);
        });

        routes.MapPost("/", (CreateBusinessResultRequest request, ClaimsPrincipal user, IBusinessResultRepository repository) =>
        {
            if (!CanAccess(user, request.ParkingLotId))
            {
                return Error("Bạn không có quyền truy cập bãi xe này.");
            }
            try
            {
                var businessResult = new ParkingBusinessResultRecord(Guid.NewGuid(), request.ParkingLotId,
                    request.ReportingPeriod, request.Revenue, request.CompletedVehicleCount, request.Note);
                return repository.TryAdd(businessResult)
                    ? Success(businessResult)
                    : Error("Bãi xe đã có dữ liệu cho kỳ báo cáo này.");
            }
            catch (ArgumentException)
            {
                return Error("Dữ liệu kết quả kinh doanh không hợp lệ.");
            }
            catch (ParkingLotNotFoundException)
            {
                return Error("Không tìm thấy bãi đỗ xe.");
            }
        });

        routes.MapPut("/{id:guid}", (Guid id, UpdateBusinessResultRequest request, ClaimsPrincipal user, IBusinessResultRepository repository) =>
        {
            ParkingBusinessResultRecord? existing = repository.FindById(id);
            if (existing is null || !CanAccess(user, existing.ParkingLotId))
            {
                return NotFound();
            }
            try
            {
                if (!repository.TryUpdate(id, request.Revenue, request.CompletedVehicleCount, request.Note))
                {
                    return NotFound();
                }

                ParkingBusinessResultRecord? updated = repository.FindById(id);
                return updated is null ? NotFound() : Success(updated);
            }
            catch (ArgumentException)
            {
                return Error("Dữ liệu kết quả kinh doanh không hợp lệ.");
            }
        });

        routes.MapDelete("/{id:guid}", (Guid id, ClaimsPrincipal user, IBusinessResultRepository repository) =>
        {
            ParkingBusinessResultRecord? businessResult = repository.FindById(id);
            return businessResult is null || !CanAccess(user, businessResult.ParkingLotId) || !repository.TryDelete(id) ? NotFound() : Success(businessResult);
        });
    }

    private static IResult Success(ParkingBusinessResultRecord businessResult) =>
        Results.Ok(ApiResponse<ParkingBusinessResultRecord>.Success(businessResult));

    private static bool CanAccess(ClaimsPrincipal user, Guid parkingLotId) =>
        user.IsInRole("ROLE_ADMIN") || user.FindAll("parking_lot_id")
            .Any(claim => Guid.TryParse(claim.Value, out Guid allowedId) && allowedId == parkingLotId);

    private static IResult NotFound() => Error("Không tìm thấy kết quả kinh doanh.");

    private static IResult Error(string message) =>
        Results.Ok(ApiResponse<ParkingBusinessResultRecord>.BusinessError(message));
}

public sealed record CreateBusinessResultRequest(
    Guid ParkingLotId, string ReportingPeriod, decimal Revenue, int CompletedVehicleCount, string? Note);

public sealed record UpdateBusinessResultRequest(decimal Revenue, int CompletedVehicleCount, string? Note);
