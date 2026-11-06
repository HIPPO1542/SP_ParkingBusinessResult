using ParkingBusinessResult.Domain;

namespace ParkingBusinessResult.Checks;

/// <summary>Temporary process-local storage. Data is lost when the process stops.</summary>
public sealed class InMemoryBusinessResultRepository : IBusinessResultRepository
{
    private readonly Lock sync = new();
    private readonly Dictionary<Guid, ParkingBusinessResultRecord> records = new();
    private readonly HashSet<Guid> deletedIds = new();

    public bool TryAdd(ParkingBusinessResultRecord businessResult, AuditActor? actor = null)
    {
        ArgumentNullException.ThrowIfNull(businessResult);
        lock (sync)
        {
            if (records.ContainsKey(businessResult.Id) || records.Values.Any(existing =>
                !deletedIds.Contains(existing.Id) && existing.ParkingLotId == businessResult.ParkingLotId &&
                existing.ReportingPeriod == businessResult.ReportingPeriod))
            {
                return false;
            }

            records.Add(businessResult.Id, Copy(businessResult));
            return true;
        }
    }

    public ParkingBusinessResultRecord? FindById(Guid id)
    {
        lock (sync)
        {
            return !deletedIds.Contains(id) && records.TryGetValue(id, out ParkingBusinessResultRecord? businessResult)
                ? Copy(businessResult)
                : null;
        }
    }

    public IReadOnlyList<ParkingBusinessResultRecord> List()
    {
        lock (sync)
        {
            return records.Values.Where(record => !deletedIds.Contains(record.Id)).Select(Copy).ToArray();
        }
    }

    public bool TryUpdate(Guid id, decimal revenue, int completedVehicleCount, string? note, AuditActor? actor = null)
    {
        lock (sync)
        {
            if (deletedIds.Contains(id) || !records.TryGetValue(id, out ParkingBusinessResultRecord? businessResult))
            {
                return false;
            }

            businessResult.Update(revenue, completedVehicleCount, note);
            return true;
        }
    }

    public bool TrySoftDelete(Guid id, AuditActor? actor = null)
    {
        lock (sync)
        {
            return records.ContainsKey(id) && deletedIds.Add(id);
        }
    }

    private static ParkingBusinessResultRecord Copy(ParkingBusinessResultRecord businessResult) =>
        new(businessResult.Id, businessResult.ParkingLotId, businessResult.ReportingPeriod,
            businessResult.Revenue, businessResult.CompletedVehicleCount, businessResult.Note);
}
