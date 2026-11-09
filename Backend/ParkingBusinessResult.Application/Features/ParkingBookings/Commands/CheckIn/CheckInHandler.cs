using ParkingBusinessResult.Application.Features.BusinessResults.Common;
using ParkingBusinessResult.Application.Features.ParkingBookings.Common;
using ParkingBusinessResult.Domain;

namespace ParkingBusinessResult.Application.Features.ParkingBookings.Commands.CheckIn;

public sealed record CheckInCommand(Guid Id, BusinessAccess Access, AuditActor Actor);

public sealed class CheckInHandler(IBookingLifecycleRepository repository, TimeProvider clock)
{
    public BookingWriteResult Handle(CheckInCommand command) =>
        repository.CheckIn(command.Id, command.Access, command.Actor, clock.GetLocalNow().DateTime);
}
