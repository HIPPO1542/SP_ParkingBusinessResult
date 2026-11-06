namespace ParkingBusinessResult.Domain;

public interface IBusinessResultRepository
{
    bool TryAdd(BusinessResultRecord businessResult, AuditActor? actor = null);
    BusinessResultRecord? FindById(Guid id);
    IReadOnlyList<BusinessResultRecord> List(IReadOnlySet<Guid>? parkingLotIds = null);
    bool TryUpdate(Guid id, decimal revenue, int completedVehicleCount, string? note, AuditActor? actor = null);
    bool TrySoftDelete(Guid id, AuditActor? actor = null);
}
