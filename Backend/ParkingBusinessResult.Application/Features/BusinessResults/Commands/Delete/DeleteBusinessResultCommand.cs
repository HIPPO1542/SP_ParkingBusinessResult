using ParkingBusinessResult.Application.Features.BusinessResults.Common;
using ParkingBusinessResult.Domain;

namespace ParkingBusinessResult.Application.Features.BusinessResults.Commands.Delete;

public sealed record DeleteBusinessResultCommand(Guid Id, BusinessAccess Access, AuditActor? Actor = null);
