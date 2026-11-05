namespace ParkingBusinessResult.Application.DTOs;

public sealed record CreateBusinessResultRequest(
    Guid ParkingLotId, string ReportingPeriod, decimal Revenue, int CompletedVehicleCount, string? Note);

public sealed record UpdateBusinessResultRequest(decimal Revenue, int CompletedVehicleCount, string? Note);
