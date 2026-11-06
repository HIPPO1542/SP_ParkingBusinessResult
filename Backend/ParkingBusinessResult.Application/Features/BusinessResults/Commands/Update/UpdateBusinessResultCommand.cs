using ParkingBusinessResult.Application.Features.BusinessResults.Common;
using ParkingBusinessResult.Domain;

namespace ParkingBusinessResult.Application.Features.BusinessResults.Commands.Update;

public sealed record UpdateBusinessResultCommand(Guid Id, decimal Revenue, int CompletedVehicleCount,
    string? Note, BusinessAccess Access, AuditActor? Actor = null);
