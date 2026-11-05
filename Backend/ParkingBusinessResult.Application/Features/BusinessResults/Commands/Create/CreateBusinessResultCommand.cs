using ParkingBusinessResult.Application.Features.BusinessResults.Common;

namespace ParkingBusinessResult.Application.Features.BusinessResults.Commands.Create;

public sealed record CreateBusinessResultCommand(Guid ParkingLotId, string ReportingPeriod, decimal Revenue,
    int CompletedVehicleCount, string? Note, BusinessAccess Access);
