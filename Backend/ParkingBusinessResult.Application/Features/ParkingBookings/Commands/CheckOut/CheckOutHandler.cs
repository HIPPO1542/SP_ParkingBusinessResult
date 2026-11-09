using ParkingBusinessResult.Application.Features.BusinessResults.Common;
using ParkingBusinessResult.Application.Features.ParkingBookings.Common;
using ParkingBusinessResult.Domain;

namespace ParkingBusinessResult.Application.Features.ParkingBookings.Commands.CheckOut;

public sealed record CheckOutCommand(Guid Id, string? DayType, BusinessAccess Access, AuditActor Actor);

public sealed class CheckOutHandler(IBookingLifecycleRepository repository, TimeProvider clock)
{
    public BookingWriteResult Handle(CheckOutCommand command) => command.DayType is not ("WEEKDAY" or "WEEKEND" or "HOLIDAY")
        ? new(null, "Cần chọn loại ngày: ngày thường, cuối tuần hoặc ngày lễ.")
        : repository.CheckOut(command.Id, command.DayType, command.Access, command.Actor, clock.GetLocalNow().DateTime);
}
