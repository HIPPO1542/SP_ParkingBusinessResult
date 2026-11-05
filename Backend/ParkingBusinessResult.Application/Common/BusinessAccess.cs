namespace ParkingBusinessResult.Application.Common;

public sealed record BusinessAccess(bool IsAdministrator, IReadOnlySet<Guid> ParkingLotIds)
{
    public bool CanAccess(Guid parkingLotId) => IsAdministrator || ParkingLotIds.Contains(parkingLotId);
}
