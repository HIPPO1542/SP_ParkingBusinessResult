using ParkingBusinessResult.Application.Features.BusinessResults.Common;

namespace ParkingBusinessResult.Application.Features.ParkingVisits.Common;

public sealed record ParkingTariffRule(decimal InitialHours, decimal InitialPrice, decimal AdditionalHourPrice);
public sealed record ParkingVisitTariffData(DateTime EnteredAt, DateTime? ExitedAt, decimal? CollectedAmount,
    IReadOnlyList<ParkingTariffRule> Rules);

public interface IParkingVisitTariffRepository
{
    ParkingVisitTariffData? Find(Guid visitId, string dayType, BusinessAccess access);
}
