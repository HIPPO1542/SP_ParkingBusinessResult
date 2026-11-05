using ParkingBusinessResult.Application.Features.BusinessResults.Common;

namespace ParkingBusinessResult.Application.Features.BusinessResults.Commands.Delete;

public sealed record DeleteBusinessResultCommand(Guid Id, BusinessAccess Access);
