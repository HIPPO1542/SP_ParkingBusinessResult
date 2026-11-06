namespace ParkingBusinessResult.Domain;

public interface IBusinessResultRepository
{
    bool TryAdd(ParkingBusinessResultRecord businessResult, AuditActor? actor = null);
    ParkingBusinessResultRecord? FindById(Guid id);
    IReadOnlyList<ParkingBusinessResultRecord> List();
    bool TryUpdate(Guid id, decimal revenue, int completedVehicleCount, string? note, AuditActor? actor = null);
    bool TrySoftDelete(Guid id, AuditActor? actor = null);
}
