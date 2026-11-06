using ParkingBusinessResult.Application.Features.BusinessResults.Common;

namespace ParkingBusinessResult.Application.Features.ParkingBookings.Queries.List;

public sealed record ListParkingBookingsQuery(Guid? ParkingLotId, string? Status, string? VehicleType, int PageNumber, int PageSize, BusinessAccess Access,
    int? Day = null, int? Month = null, int? Year = null, string DateField = "enteredAt");
