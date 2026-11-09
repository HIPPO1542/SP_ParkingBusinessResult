using System.Security.Claims;
using ParkingBusinessResult.Api.Contracts;
using ParkingBusinessResult.Application.Features.ParkingBookings.Common;
using ParkingBusinessResult.Application.Features.ParkingBookings.Queries.MyBookings;

namespace ParkingBusinessResult.Api.Endpoints.ParkingBookings;

public static class GetMyBookingsEndpoint
{
    public static void Map(RouteGroupBuilder routes) => routes.MapGet("/my", Handle);

    private static IResult Handle(int? pageNumber, int? pageSize, HttpRequest request, ClaimsPrincipal user, GetMyBookingsHandler handler)
    {
        int page = pageNumber ?? 1;
        int size = pageSize ?? 10;
        if (page < 1 || size is < 1 or > 100 || request.Query.Keys.Any(key => key is not ("pageNumber" or "pageSize")))
            return Results.Ok(ApiResponse<object>.BusinessError("Bộ lọc hoặc phân trang không hợp lệ."));
        CustomerBookingPage result = handler.Handle(new GetMyBookingsQuery(EndpointSupport.GetAuditActor(user).Id, page, size));
        return Results.Ok(ApiResponse<CustomerBookingPage>.Success(result));
    }
}
