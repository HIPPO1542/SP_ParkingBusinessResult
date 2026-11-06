using ParkingBusinessResult.Application.Features.BusinessResults.Common;
using ParkingBusinessResult.Domain;

namespace ParkingBusinessResult.Application.Features.BusinessResults.Commands.Update;

public sealed class UpdateBusinessResultHandler(IBusinessResultRepository repository)
{
    public BusinessResultOutcome<BusinessResultRecord> Handle(UpdateBusinessResultCommand command)
    {
        BusinessResultRecord? existing = repository.FindById(command.Id);
        if (existing is null || !command.Access.CanAccess(existing.ParkingLotId)) return NotFound();

        try
        {
            existing.Update(command.Revenue, command.CompletedVehicleCount, command.Note);
            if (!repository.TryUpdate(command.Id, existing.Revenue, existing.CompletedVehicleCount, existing.Note, command.Actor)) return NotFound();
            BusinessResultRecord? updated = repository.FindById(command.Id);
            return updated is null ? NotFound() : BusinessResultOutcome<BusinessResultRecord>.Success(updated);
        }
        catch (ArgumentException)
        {
            return BusinessResultOutcome<BusinessResultRecord>.Failure("Dữ liệu kết quả kinh doanh không hợp lệ.");
        }
    }

    private static BusinessResultOutcome<BusinessResultRecord> NotFound() =>
        BusinessResultOutcome<BusinessResultRecord>.Failure("Không tìm thấy kết quả kinh doanh.");
}
