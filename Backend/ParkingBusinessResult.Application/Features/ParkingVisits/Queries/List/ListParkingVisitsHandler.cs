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
            .Where(visit => MatchesDate(visit, query))
            .OrderByDescending(visit => visit.EnteredAt).ThenBy(visit => visit.Id).ToArray();
        long offset = ((long)query.PageNumber - 1) * query.PageSize;
        ParkingVisitItem[] page = offset >= visits.Length ? [] : visits.Skip((int)offset).Take(query.PageSize).ToArray();
        return new(page, visits.Length, query.PageNumber, query.PageSize);
    }

    private static bool MatchesDate(ParkingVisitItem visit, ListParkingVisitsQuery query)
    {
        if (query.Day is null && query.Month is null && query.Year is null) return true;
        DateTime? date = query.DateField == "exitedAt" ? visit.ExitedAt : visit.EnteredAt;
        return date.HasValue && (query.Day is null || date.Value.Day == query.Day) &&
            (query.Month is null || date.Value.Month == query.Month) &&
            (query.Year is null || date.Value.Year == query.Year);
    }
}
