namespace ParkingBusinessResult.Application.DTOs;

public sealed record LoginRequest(string Username, string Password);
public sealed record CreateAccountRequest(string Username, string Password, string Role, Guid? ParkingLotId);
