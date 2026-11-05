namespace ParkingBusinessResult.Application.Features.DevelopmentAccounts.Common;

public sealed record DevelopmentAccount(Guid Id, string Username, string PasswordHash, string Role, Guid? ParkingLotId);
