using ParkingBusinessResult.Application.Common;
using ParkingBusinessResult.Domain;

namespace ParkingBusinessResult.Application.Features.BusinessResults.Commands.Update;

public sealed class UpdateBusinessResultHandler(IBusinessResultRepository repository)
{
    public BusinessResultOutcome<ParkingBusinessResultRecord> Handle(UpdateBusinessResultCommand command)
    {
        ParkingBusinessResultRecord? existing = repository.FindById(command.Id);
        if (existing is null || !command.Access.CanAccess(existing.ParkingLotId)) return NotFound();

        try
        {
            if (!repository.TryUpdate(command.Id, command.Revenue, command.CompletedVehicleCount, command.Note)) return NotFound();
            ParkingBusinessResultRecord? updated = repository.FindById(command.Id);
            return updated is null ? NotFound() : BusinessResultOutcome<ParkingBusinessResultRecord>.Success(updated);
        }
        catch (ArgumentException)
        {
            return BusinessResultOutcome<ParkingBusinessResultRecord>.Failure("Dữ liệu kết quả kinh doanh không hợp lệ.");
        }
    }

    private static BusinessResultOutcome<ParkingBusinessResultRecord> NotFound() =>
        BusinessResultOutcome<ParkingBusinessResultRecord>.Failure("Không tìm thấy kết quả kinh doanh.");
}
