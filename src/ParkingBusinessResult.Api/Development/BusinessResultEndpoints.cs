using ParkingBusinessResult.Api.Contracts;
using ParkingBusinessResult.Domain;

namespace ParkingBusinessResult.Api.Development;

public static class BusinessResultEndpoints
{
    public static void Map(WebApplication app)
    {
        RouteGroupBuilder routes = app.MapGroup("/dev/business-results");

        routes.MapGet("/", (IBusinessResultRepository repository) =>
            Results.Ok(ApiResponse<IReadOnlyList<ParkingBusinessResultRecord>>.Success(repository.List())));

        routes.MapGet("/{id:guid}", (Guid id, IBusinessResultRepository repository) =>
        {
            ParkingBusinessResultRecord? businessResult = repository.FindById(id);
            return businessResult is null ? NotFound() : Success(businessResult);
        });

        routes.MapPost("/", (CreateBusinessResultRequest request, IBusinessResultRepository repository) =>
        {
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
        });

        routes.MapPut("/{id:guid}", (Guid id, UpdateBusinessResultRequest request, IBusinessResultRepository repository) =>
        {
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

        routes.MapDelete("/{id:guid}", (Guid id, IBusinessResultRepository repository) =>
        {
            ParkingBusinessResultRecord? businessResult = repository.FindById(id);
            return businessResult is null || !repository.TryDelete(id) ? NotFound() : Success(businessResult);
        });
    }

    private static IResult Success(ParkingBusinessResultRecord businessResult) =>
        Results.Ok(ApiResponse<ParkingBusinessResultRecord>.Success(businessResult));

    private static IResult NotFound() => Error("Không tìm thấy kết quả kinh doanh.");

    private static IResult Error(string message) =>
        Results.Ok(ApiResponse<ParkingBusinessResultRecord>.BusinessError(message));
}

public sealed record CreateBusinessResultRequest(
    Guid ParkingLotId, string ReportingPeriod, decimal Revenue, int CompletedVehicleCount, string? Note);

public sealed record UpdateBusinessResultRequest(decimal Revenue, int CompletedVehicleCount, string? Note);
