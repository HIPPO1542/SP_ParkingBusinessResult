using ParkingBusinessResult.Api.Contracts;
using ParkingBusinessResult.Application.Features.LocalAccounts.DTOs;
using ParkingBusinessResult.Application.Features.LocalAccounts.Commands.SignIn;

namespace ParkingBusinessResult.Api.Endpoints.LocalAccounts;

public static class LoginEndpoint
{
    public static void Map(RouteGroupBuilder routes) =>
        routes.MapPost("/login", Handle).AllowAnonymous().RequireRateLimiting("login");

    private static IResult Handle(LoginRequest request, SignInHandler handler)
    {
        string? token = handler.Handle(new SignInCommand(request.Username, request.Password));
        return token is null
            ? Results.Ok(ApiResponse<object>.BusinessError("Tên đăng nhập hoặc mật khẩu không đúng."))
            : Results.Ok(ApiResponse<object>.Success(new { accessToken = token, expiresIn = 1800 }));
    }
}
