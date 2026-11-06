using ParkingBusinessResult.Application.Features.BusinessResults.Common;
using ParkingBusinessResult.Domain;

namespace ParkingBusinessResult.Application.Features.BusinessResults.Commands.Create;

public sealed class CreateBusinessResultHandler(IBusinessResultRepository repository)
{
    public BusinessResultOutcome<BusinessResultRecord> Handle(CreateBusinessResultCommand command)
    {
        if (!command.Access.CanAccess(command.ParkingLotId))
            return BusinessResultOutcome<BusinessResultRecord>.Failure("Bạn không có quyền truy cập bãi xe này.");

        try
        {
            var result = new BusinessResultRecord(Guid.NewGuid(), command.ParkingLotId, command.ReportingPeriod,
                command.Revenue, command.CompletedVehicleCount, command.Note);
            return repository.TryAdd(result, command.Actor)
                ? BusinessResultOutcome<BusinessResultRecord>.Success(result)
                : BusinessResultOutcome<BusinessResultRecord>.Failure("Bãi xe đã có dữ liệu cho kỳ báo cáo này.");
        }
        catch (ArgumentException)
        {
            return BusinessResultOutcome<BusinessResultRecord>.Failure("Dữ liệu kết quả kinh doanh không hợp lệ.");
        }
        catch (ParkingLotNotFoundException)
        {
            return BusinessResultOutcome<BusinessResultRecord>.Failure("Không tìm thấy bãi đỗ xe.");
        }
    }
}
