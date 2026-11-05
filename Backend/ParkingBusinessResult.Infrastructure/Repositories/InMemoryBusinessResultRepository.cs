using ParkingBusinessResult.Domain;

namespace ParkingBusinessResult.Infrastructure;

/// <summary>Temporary process-local storage. Data is lost when the process stops.</summary>
public sealed class InMemoryBusinessResultRepository : IBusinessResultRepository
{
    private readonly Lock sync = new();
    private readonly Dictionary<Guid, ParkingBusinessResultRecord> records = new();

    public bool TryAdd(ParkingBusinessResultRecord businessResult)
    {
        ArgumentNullException.ThrowIfNull(businessResult);
        lock (sync)
        {
            if (records.ContainsKey(businessResult.Id) || records.Values.Any(existing =>
                existing.ParkingLotId == businessResult.ParkingLotId &&
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
            return records.TryGetValue(id, out ParkingBusinessResultRecord? businessResult)
                ? Copy(businessResult)
                : null;
        }
    }

    public IReadOnlyList<ParkingBusinessResultRecord> List()
    {
        lock (sync)
        {
            return records.Values.Select(Copy).ToArray();
        }
    }

    public bool TryUpdate(Guid id, decimal revenue, int completedVehicleCount, string? note)
    {
        lock (sync)
        {
            if (!records.TryGetValue(id, out ParkingBusinessResultRecord? businessResult))
            {
                return false;
            }

            businessResult.Update(revenue, completedVehicleCount, note);
            return true;
        }
    }

    public bool TryDelete(Guid id)
    {
        lock (sync)
        {
            return records.Remove(id);
        }
    }

    private static ParkingBusinessResultRecord Copy(ParkingBusinessResultRecord businessResult) =>
        new(businessResult.Id, businessResult.ParkingLotId, businessResult.ReportingPeriod,
            businessResult.Revenue, businessResult.CompletedVehicleCount, businessResult.Note);
}
