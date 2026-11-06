namespace ParkingBusinessResult.Application.Features.LocalAccounts.Common;

public sealed record LocalAccount(Guid Id, string Username, string PasswordHash, string Role, Guid? ParkingLotId);
