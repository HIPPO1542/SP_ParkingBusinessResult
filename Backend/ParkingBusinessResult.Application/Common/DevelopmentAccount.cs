namespace ParkingBusinessResult.Application.Common;

public sealed record DevelopmentAccount(Guid Id, string Username, string PasswordHash, string Role, Guid? ParkingLotId);
