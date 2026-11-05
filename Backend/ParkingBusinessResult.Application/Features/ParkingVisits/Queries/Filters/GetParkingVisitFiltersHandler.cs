using ParkingBusinessResult.Application.Features.BusinessReports.Common;
using ParkingBusinessResult.Application.Features.BusinessResults.Common;
using ParkingBusinessResult.Application.Features.ParkingVisits.Common;

namespace ParkingBusinessResult.Application.Features.ParkingVisits.Queries.Filters;

public sealed record GetParkingVisitFiltersQuery(BusinessAccess Access);
public sealed record ParkingVisitFilters(IReadOnlyList<ReportParkingLot> ParkingLots, IReadOnlyList<string> VehicleTypes);

public sealed class GetParkingVisitFiltersHandler(IParkingVisitListRepository visits, IBusinessReportParkingLotSearch parkingLots)
{
    public ParkingVisitFilters Handle(GetParkingVisitFiltersQuery query)
    {
        HashSet<Guid> permittedIds = parkingLots.FindMatchingLots(null, null).Where(query.Access.CanAccess).ToHashSet();
        ReportParkingLot[] lots = parkingLots.GetParkingLots(permittedIds).Values
            .OrderBy(lot => lot.Code, StringComparer.OrdinalIgnoreCase).ThenBy(lot => lot.Name, StringComparer.OrdinalIgnoreCase).ToArray();
        string[] vehicleTypes = visits.List().Where(visit => query.Access.CanAccess(visit.ParkingLotId))
            .Select(visit => visit.VehicleType).Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(type => type, StringComparer.OrdinalIgnoreCase).ToArray();
        return new(lots, vehicleTypes);
    }
}
