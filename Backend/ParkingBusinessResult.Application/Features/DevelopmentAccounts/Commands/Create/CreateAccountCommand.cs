namespace ParkingBusinessResult.Application.Features.DevelopmentAccounts.Commands.Create;

public sealed record CreateAccountCommand(string Username, string Password, string Role, Guid? ParkingLotId);
