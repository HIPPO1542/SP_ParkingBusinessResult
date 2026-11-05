using ParkingBusinessResult.Application.Features.BusinessResults.Common;
using ParkingBusinessResult.Domain;

namespace ParkingBusinessResult.Application.Features.BusinessResults.Queries.GetById;

public sealed class GetBusinessResultHandler(IBusinessResultRepository repository)
{
    public BusinessResultOutcome<ParkingBusinessResultRecord> Handle(GetBusinessResultQuery query)
    {
        ParkingBusinessResultRecord? result = repository.FindById(query.Id);
        return result is null || !query.Access.CanAccess(result.ParkingLotId)
            ? BusinessResultOutcome<ParkingBusinessResultRecord>.Failure("Không tìm thấy kết quả kinh doanh.")
            : BusinessResultOutcome<ParkingBusinessResultRecord>.Success(result);
    }
}
