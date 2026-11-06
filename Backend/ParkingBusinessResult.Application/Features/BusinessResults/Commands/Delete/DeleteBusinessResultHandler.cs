using ParkingBusinessResult.Application.Features.BusinessResults.Common;
using ParkingBusinessResult.Domain;

namespace ParkingBusinessResult.Application.Features.BusinessResults.Commands.Delete;

public sealed class DeleteBusinessResultHandler(IBusinessResultRepository repository)
{
    public BusinessResultOutcome<ParkingBusinessResultRecord> Handle(DeleteBusinessResultCommand command)
    {
        ParkingBusinessResultRecord? result = repository.FindById(command.Id);
        if (result is null || !command.Access.CanAccess(result.ParkingLotId) || !repository.TrySoftDelete(command.Id, command.Actor))
            return BusinessResultOutcome<ParkingBusinessResultRecord>.Failure("Không tìm thấy kết quả kinh doanh.");

        return BusinessResultOutcome<ParkingBusinessResultRecord>.Success(result);
    }
}
