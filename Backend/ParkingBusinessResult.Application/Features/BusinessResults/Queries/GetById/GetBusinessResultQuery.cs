using ParkingBusinessResult.Application.Features.BusinessResults.Common;

namespace ParkingBusinessResult.Application.Features.BusinessResults.Queries.GetById;

public sealed record GetBusinessResultQuery(Guid Id, BusinessAccess Access);
