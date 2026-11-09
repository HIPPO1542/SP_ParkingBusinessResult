using ParkingBusinessResult.Application.Features.ParkingBookings.Common;
using ParkingBusinessResult.Domain;

namespace ParkingBusinessResult.Application.Features.ParkingBookings.Commands.Reserve;

public sealed record ReserveBookingCommand(ReservationInput Input, AuditActor Actor);

public sealed class ReserveBookingHandler(IBookingLifecycleRepository repository, TimeProvider clock)
{
    public BookingWriteResult Handle(ReserveBookingCommand command) =>
        repository.Reserve(command.Input, command.Actor, clock.GetLocalNow().DateTime);
}
