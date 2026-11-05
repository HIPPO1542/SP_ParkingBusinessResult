namespace ParkingBusinessResult.Domain;

public interface IBusinessResultRepository
{
    bool TryAdd(ParkingBusinessResultRecord businessResult);
    ParkingBusinessResultRecord? FindById(Guid id);
    IReadOnlyList<ParkingBusinessResultRecord> List();
    bool TryUpdate(Guid id, decimal revenue, int completedVehicleCount, string? note);
    bool TrySoftDelete(Guid id);
}
