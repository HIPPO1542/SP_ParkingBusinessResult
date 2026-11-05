using System.Globalization;
using System.Security.Claims;
using ParkingBusinessResult.Api.Contracts;
using ParkingBusinessResult.Application.Features.ParkingVisits.Queries.List;

namespace ParkingBusinessResult.Api.Endpoint.ParkingVisits;

public static class ListParkingVisitsEndpoint
{
    public static void Map(RouteGroupBuilder routes) => routes.MapGet("/", Handle);

    private static IResult Handle(Guid? parkingLotId, string? status, string? vehicleType, string? pageNumber, string? pageSize,
        HttpRequest request, ClaimsPrincipal user, ListParkingVisitsHandler handler)
    {
        string[] allowed = ["parkingLotId", "status", "vehicleType", "pageNumber", "pageSize"];
        if (request.Query.Keys.Any(key => !allowed.Contains(key, StringComparer.OrdinalIgnoreCase)) ||
            (status is not null && status is not ("PARKED" or "COMPLETED")) ||
            (vehicleType is not null && (string.IsNullOrWhiteSpace(vehicleType) || vehicleType.Length > 100)) ||
            !ReadPositive(pageNumber, 1, out int page) || !ReadPositive(pageSize, 10, out int size) || size > 100)
            return Results.Ok(ApiResponse<object>.BusinessError("Bộ lọc hoặc phân trang không hợp lệ."));
        ParkingVisitPage result = handler.Handle(new ListParkingVisitsQuery(parkingLotId, status, vehicleType?.Trim(), page, size, EndpointSupport.GetAccess(user)));
        return Results.Ok(ApiResponse<ParkingVisitPage>.Success(result));
    }

    private static bool ReadPositive(string? input, int defaultValue, out int value)
    {
        if (input is null) { value = defaultValue; return true; }
        return int.TryParse(input, NumberStyles.None, CultureInfo.InvariantCulture, out value) && value > 0;
    }
}
