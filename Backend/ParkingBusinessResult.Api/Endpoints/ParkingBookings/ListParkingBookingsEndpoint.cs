using System.Globalization;
using System.Security.Claims;
using ParkingBusinessResult.Api.Contracts;
using ParkingBusinessResult.Application.Features.ParkingBookings.Queries.List;

namespace ParkingBusinessResult.Api.Endpoints.ParkingBookings;

public static class ListParkingBookingsEndpoint
{
    public static void Map(RouteGroupBuilder routes) => routes.MapGet("/", Handle);

    private static IResult Handle(Guid? parkingLotId, string? status, string? vehicleType, string? pageNumber, string? pageSize,
        string? day, string? month, string? year, string? dateField,
        HttpRequest request, ClaimsPrincipal user, ListParkingBookingsHandler handler)
    {
        string[] allowed = ["parkingLotId", "status", "vehicleType", "pageNumber", "pageSize", "day", "month", "year", "dateField"];
        if (request.Query.Keys.Any(key => !allowed.Contains(key, StringComparer.OrdinalIgnoreCase)) ||
            (status is not null && status is not ("PARKED" or "COMPLETED")) ||
            (vehicleType is not null && (string.IsNullOrWhiteSpace(vehicleType) || vehicleType.Length > 100)) ||
            !ReadOptional(day, 31, out int? dayNumber) || !ReadOptional(month, 12, out int? monthNumber) ||
            !ReadOptional(year, 9999, out int? yearNumber) ||
            (dateField is not null && dateField is not ("enteredAt" or "exitedAt")) ||
            !ReadPositive(pageNumber, 1, out int page) || !ReadPositive(pageSize, 10, out int size) || size > 100)
            return Results.Ok(ApiResponse<object>.BusinessError("Bộ lọc hoặc phân trang không hợp lệ."));
        if (dayNumber.HasValue && monthNumber.HasValue && dayNumber > DateTime.DaysInMonth(yearNumber ?? 2000, monthNumber.Value))
            return Results.Ok(ApiResponse<object>.BusinessError("Ngày không tồn tại trong tháng đã chọn."));
        ParkingBookingPage result = handler.Handle(new ListParkingBookingsQuery(parkingLotId, status, vehicleType?.Trim(), page, size, EndpointSupport.GetAccess(user), dayNumber, monthNumber, yearNumber, dateField ?? "enteredAt"));
        return Results.Ok(ApiResponse<ParkingBookingPage>.Success(result));
    }

    private static bool ReadPositive(string? input, int defaultValue, out int value)
    {
        if (input is null) { value = defaultValue; return true; }
        return int.TryParse(input, NumberStyles.None, CultureInfo.InvariantCulture, out value) && value > 0;
    }

    private static bool ReadOptional(string? input, int maximum, out int? value)
    {
        value = null;
        if (input is null) return true;
        if (!int.TryParse(input, NumberStyles.None, CultureInfo.InvariantCulture, out int parsed) || parsed < 1 || parsed > maximum) return false;
        value = parsed;
        return true;
    }
}
