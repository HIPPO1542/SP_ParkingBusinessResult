using ParkingBusinessResult.Application.Features.BusinessResults.Common;
using ParkingBusinessResult.Application.Features.ParkingBookings.Common;
using ParkingBusinessResult.Domain;

namespace ParkingBusinessResult.Application.Features.ParkingBookings.Commands.Cancel;

public sealed record CancelBookingCommand(Guid Id, string? Note, bool IsCustomer, BusinessAccess Access, AuditActor Actor);

public sealed class CancelBookingHandler(IBookingLifecycleRepository repository)
{
    public BookingWriteResult Handle(CancelBookingCommand command) =>
        repository.Cancel(command.Id, command.Note, command.IsCustomer ? command.Actor.Id : null, command.Access, command.Actor);
}
