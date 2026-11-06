using ParkingBusinessResult.Application.Features.BusinessReports.Common;
using ParkingBusinessResult.Application.Features.BusinessResults.Common;
using ParkingBusinessResult.Application.Features.ParkingBookings.Common;

namespace ParkingBusinessResult.Application.Features.ParkingBookings.Queries.Filters;

public sealed record GetParkingBookingFiltersQuery(BusinessAccess Access);
public sealed record ParkingBookingFilters(IReadOnlyList<ReportParkingLot> ParkingLots, IReadOnlyList<string> VehicleTypes);

public sealed class GetParkingBookingFiltersHandler(IParkingBookingListRepository bookings, IBusinessReportParkingLotSearch parkingLots)
{
    public ParkingBookingFilters Handle(GetParkingBookingFiltersQuery query)
    {
        HashSet<Guid> permittedIds = parkingLots.FindMatchingLots(null, null).Where(query.Access.CanAccess).ToHashSet();
        ReportParkingLot[] lots = parkingLots.GetParkingLots(permittedIds).Values
            .OrderBy(lot => lot.Code, StringComparer.OrdinalIgnoreCase).ThenBy(lot => lot.Name, StringComparer.OrdinalIgnoreCase).ToArray();
        string[] vehicleTypes = bookings.List().Where(booking => query.Access.CanAccess(booking.ParkingLotId))
            .Select(booking => booking.VehicleType).Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(type => type, StringComparer.OrdinalIgnoreCase).ToArray();
        return new(lots, vehicleTypes);
    }
}
