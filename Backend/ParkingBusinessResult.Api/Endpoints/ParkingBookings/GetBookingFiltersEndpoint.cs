using System.Security.Claims;
using ParkingBusinessResult.Api.Contracts;
using ParkingBusinessResult.Application.Features.ParkingBookings.Queries.Filters;

namespace ParkingBusinessResult.Api.Endpoints.ParkingBookings;

public static class GetBookingFiltersEndpoint
{
    public static void Map(RouteGroupBuilder routes) => routes.MapGet("/filters", Handle);

    private static IResult Handle(int? day, int? month, int? year, HttpRequest request, ClaimsPrincipal user, GetBookingFiltersHandler handler)
    {
        string[] allowed = ["day", "month", "year"];
        if (request.Query.Keys.Any(key => !allowed.Contains(key, StringComparer.OrdinalIgnoreCase)) ||
            day is < 1 or > 31 || month is < 1 or > 12 || year is < 1 or > 9999)
            return Results.Ok(ApiResponse<object>.BusinessError("Bộ lọc ngày/tháng/năm không hợp lệ."));
        if (day.HasValue && (!month.HasValue || !year.HasValue))
            return Results.Ok(ApiResponse<object>.BusinessError("Chọn ngày thì phải chọn đủ tháng và năm."));
        if (month.HasValue && !year.HasValue)
            return Results.Ok(ApiResponse<object>.BusinessError("Bạn chưa chọn năm."));
        if (day.HasValue && day > DateTime.DaysInMonth(year!.Value, month!.Value))
            return Results.Ok(ApiResponse<object>.BusinessError("Ngày không tồn tại trong tháng và năm đã chọn."));
        BookingFilters filters = handler.Handle(new GetBookingFiltersQuery(EndpointSupport.GetAccess(user), day, month, year));
        return Results.Ok(ApiResponse<BookingFilters>.Success(filters));
    }
}
