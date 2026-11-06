using ParkingBusinessResult.Application.Features.BusinessResults.Common;

namespace ParkingBusinessResult.Application.Features.ParkingBookings.Commands.Import;

public sealed record ImportParkingBookingsCommand(Stream Content, string Extension, BusinessAccess Access,
    string? UserId = null, string? Username = null);
