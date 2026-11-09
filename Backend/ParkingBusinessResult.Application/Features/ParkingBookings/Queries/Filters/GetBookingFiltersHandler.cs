using ParkingBusinessResult.Application.Features.BusinessReports.Common;
using ParkingBusinessResult.Application.Features.BusinessResults.Common;
using ParkingBusinessResult.Application.Features.ParkingBookings.Common;

namespace ParkingBusinessResult.Application.Features.ParkingBookings.Queries.Filters;

public sealed record GetBookingFiltersQuery(BusinessAccess Access);
public sealed record BookingFilters(IReadOnlyList<ReportParkingLot> ParkingLots, IReadOnlyList<string> VehicleTypes);

public sealed class GetBookingFiltersHandler(IBookingListRepository bookings, IReportParkingLotSearch parkingLots)
{
    public BookingFilters Handle(GetBookingFiltersQuery query)
    {
        HashSet<Guid> permittedIds = parkingLots.FindMatchingLots(null, null).Where(query.Access.CanAccess).ToHashSet();
        ReportParkingLot[] lots = parkingLots.GetParkingLots(permittedIds).Values
            .OrderBy(lot => lot.Code, StringComparer.OrdinalIgnoreCase).ThenBy(lot => lot.Name, StringComparer.OrdinalIgnoreCase).ToArray();
        IReadOnlyList<string> vehicleTypes = bookings.GetVehicleTypes(query.Access);
        return new(lots, vehicleTypes);
    }
}
