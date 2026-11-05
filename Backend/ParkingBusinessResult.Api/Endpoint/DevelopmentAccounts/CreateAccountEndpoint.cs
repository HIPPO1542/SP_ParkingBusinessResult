using ParkingBusinessResult.Api.Contracts;
using ParkingBusinessResult.Application.DTOs;
using ParkingBusinessResult.Application.Features.DevelopmentAccounts.Commands.Create;

namespace ParkingBusinessResult.Api.Endpoint.DevelopmentAccounts;

public static class CreateAccountEndpoint
{
    public static void Map(RouteGroupBuilder routes) =>
        routes.MapPost("/users", Handle).RequireAuthorization("DevelopmentAccountAdmin");

    private static IResult Handle(CreateAccountRequest request, CreateAccountHandler handler)
    {
        bool created = handler.Handle(new CreateAccountCommand(request.Username, request.Password, request.Role, request.ParkingLotId));
        return created
            ? Results.Ok(ApiResponse<object>.Success(new { username = request.Username }))
            : Results.Ok(ApiResponse<object>.BusinessError("Thông tin tài khoản không hợp lệ hoặc tên đăng nhập đã tồn tại."));
    }
}
