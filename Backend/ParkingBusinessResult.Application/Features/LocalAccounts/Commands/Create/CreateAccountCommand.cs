namespace ParkingBusinessResult.Application.Features.LocalAccounts.Commands.Create;

public sealed record CreateAccountCommand(string Username, string Password, string Role, Guid? ParkingLotId);
