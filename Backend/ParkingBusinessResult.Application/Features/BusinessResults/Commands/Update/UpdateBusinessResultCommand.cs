using ParkingBusinessResult.Application.Common;

namespace ParkingBusinessResult.Application.Features.BusinessResults.Commands.Update;

public sealed record UpdateBusinessResultCommand(Guid Id, decimal Revenue, int CompletedVehicleCount,
    string? Note, BusinessAccess Access);
