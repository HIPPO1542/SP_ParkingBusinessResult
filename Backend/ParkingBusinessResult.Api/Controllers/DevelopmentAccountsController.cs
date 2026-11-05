using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ParkingBusinessResult.Api.Contracts;
using ParkingBusinessResult.Application.DTOs;
using ParkingBusinessResult.Application.Features.DevelopmentAccounts.Commands.Create;
using ParkingBusinessResult.Application.Features.DevelopmentAccounts.Commands.SignIn;

namespace ParkingBusinessResult.Api.Controllers;

[Route("dev/auth")]
public sealed class DevelopmentAccountsController : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    public IActionResult Login([FromBody] LoginRequest request, [FromServices] SignInHandler handler)
    {
        string? token = handler.Handle(new SignInCommand(request.Username, request.Password));
        return token is null
            ? Ok(ApiResponse<object>.BusinessError("Tên đăng nhập hoặc mật khẩu không đúng."))
            : Ok(ApiResponse<object>.Success(new { accessToken = token, expiresIn = 1800 }));
    }

    [HttpPost("users")]
    [Authorize(Policy = "DevelopmentAccountAdmin")]
    public IActionResult CreateUser([FromBody] CreateAccountRequest request, [FromServices] CreateAccountHandler handler)
    {
        bool created = handler.Handle(new CreateAccountCommand(request.Username, request.Password, request.Role, request.ParkingLotId));
        return created
            ? Ok(ApiResponse<object>.Success(new { username = request.Username }))
            : Ok(ApiResponse<object>.BusinessError("Thông tin tài khoản không hợp lệ hoặc tên đăng nhập đã tồn tại."));
    }
}
