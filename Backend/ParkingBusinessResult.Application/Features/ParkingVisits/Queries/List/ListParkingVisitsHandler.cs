using ParkingBusinessResult.Application.Features.ParkingVisits.Common;

namespace ParkingBusinessResult.Application.Features.ParkingVisits.Queries.List;

public sealed record ParkingVisitPage(IReadOnlyList<ParkingVisitItem> Items, int TotalCount, int PageNumber, int PageSize);

public sealed class ListParkingVisitsHandler(IParkingVisitListRepository repository)
{
    public ParkingVisitPage Handle(ListParkingVisitsQuery query)
    {
        ParkingVisitItem[] visits = repository.List()
            .Where(visit => query.Access.CanAccess(visit.ParkingLotId))
            .Where(visit => query.ParkingLotId is null || visit.ParkingLotId == query.ParkingLotId)
            .Where(visit => query.Status is null || (query.Status == "PARKED" ? visit.ExitedAt is null : visit.ExitedAt is not null))
            .Where(visit => query.VehicleType is null || string.Equals(visit.VehicleType, query.VehicleType, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(visit => visit.EnteredAt).ThenBy(visit => visit.Id).ToArray();
        long offset = ((long)query.PageNumber - 1) * query.PageSize;
        ParkingVisitItem[] page = offset >= visits.Length ? [] : visits.Skip((int)offset).Take(query.PageSize).ToArray();
        return new(page, visits.Length, query.PageNumber, query.PageSize);
    }
}
