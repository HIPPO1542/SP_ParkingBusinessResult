using ParkingBusinessResult.Api.Contracts;
using ParkingBusinessResult.Application.Features.LocalAccounts.Commands.Register;
using ParkingBusinessResult.Application.Features.LocalAccounts.DTOs;

namespace ParkingBusinessResult.Api.Endpoints.LocalAccounts;

public static class RegisterCustomerEndpoint
{
    public static void Map(RouteGroupBuilder routes) =>
        routes.MapPost("/register", Handle).AllowAnonymous().RequireRateLimiting("login");

    private static IResult Handle(LoginRequest request, RegisterCustomerHandler handler)
    {
        RegisteredCustomer? customer = handler.Handle(new RegisterCustomerCommand(request.Username, request.Password));
        return customer is null
            ? Results.Ok(ApiResponse<object>.BusinessError("Tên tài khoản đã tồn tại hoặc thông tin không hợp lệ. Mật khẩu cần từ 12 đến 256 ký tự."))
            : Results.Ok(ApiResponse<RegisteredCustomer>.Success(customer, "Đăng ký khách hàng thành công."));
    }
}
