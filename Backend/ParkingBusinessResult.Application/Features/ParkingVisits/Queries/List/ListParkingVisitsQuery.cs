using ParkingBusinessResult.Application.Features.BusinessResults.Common;

namespace ParkingBusinessResult.Application.Features.ParkingVisits.Queries.List;

public sealed record ListParkingVisitsQuery(Guid? ParkingLotId, string? Status, int PageNumber, int PageSize, BusinessAccess Access);
