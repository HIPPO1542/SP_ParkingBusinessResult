using ParkingBusinessResult.Application.Features.BusinessResults.Common;
using ParkingBusinessResult.Domain;

namespace ParkingBusinessResult.Application.Features.BusinessResults.Commands.Create;

public sealed class CreateBusinessResultHandler(IBusinessResultRepository repository)
{
    public BusinessResultOutcome<ParkingBusinessResultRecord> Handle(CreateBusinessResultCommand command)
    {
        if (!command.Access.CanAccess(command.ParkingLotId))
            return BusinessResultOutcome<ParkingBusinessResultRecord>.Failure("Bạn không có quyền truy cập bãi xe này.");

        try
        {
            var result = new ParkingBusinessResultRecord(Guid.NewGuid(), command.ParkingLotId, command.ReportingPeriod,
                command.Revenue, command.CompletedVehicleCount, command.Note);
            return repository.TryAdd(result, command.Actor)
                ? BusinessResultOutcome<ParkingBusinessResultRecord>.Success(result)
                : BusinessResultOutcome<ParkingBusinessResultRecord>.Failure("Bãi xe đã có dữ liệu cho kỳ báo cáo này.");
        }
        catch (ArgumentException)
        {
            return BusinessResultOutcome<ParkingBusinessResultRecord>.Failure("Dữ liệu kết quả kinh doanh không hợp lệ.");
        }
        catch (ParkingLotNotFoundException)
        {
            return BusinessResultOutcome<ParkingBusinessResultRecord>.Failure("Không tìm thấy bãi đỗ xe.");
        }
    }
}
