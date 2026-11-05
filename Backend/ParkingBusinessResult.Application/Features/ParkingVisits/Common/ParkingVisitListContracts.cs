namespace ParkingBusinessResult.Application.Features.ParkingVisits.Common;

public sealed record ParkingVisitItem(Guid Id, Guid ParkingLotId, string LicensePlate, string VehicleType,
    DateTime EnteredAt, DateTime? ExitedAt, decimal? CollectedAmount, string? PaymentMethod,
    string? ParkingLotCode, string? ParkingLotName);

public interface IParkingVisitListRepository
{
    IReadOnlyList<ParkingVisitItem> List();
}
