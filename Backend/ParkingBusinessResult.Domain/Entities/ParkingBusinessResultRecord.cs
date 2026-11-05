namespace ParkingBusinessResult.Domain;

/// <summary>A parking lot's business result for one reporting period.</summary>
public sealed class ParkingBusinessResultRecord
{
    public Guid Id { get; }
    public Guid ParkingLotId { get; }
    public string ReportingPeriod { get; }
    public decimal Revenue { get; private set; }
    public int CompletedVehicleCount { get; private set; }
    public string? Note { get; private set; }

    public ParkingBusinessResultRecord(
        Guid id,
        Guid parkingLotId,
        string reportingPeriod,
        decimal revenue,
        int completedVehicleCount,
        string? note)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("A record ID is required.", nameof(id));
        }

        if (parkingLotId == Guid.Empty)
        {
            throw new ArgumentException("A parking lot ID is required.", nameof(parkingLotId));
        }

        if (string.IsNullOrWhiteSpace(reportingPeriod))
        {
            throw new ArgumentException("A reporting period is required.", nameof(reportingPeriod));
        }

        Id = id;
        ParkingLotId = parkingLotId;
        ReportingPeriod = reportingPeriod;
        Update(revenue, completedVehicleCount, note);
    }

    public void Update(decimal revenue, int completedVehicleCount, string? note)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(revenue);
        ArgumentOutOfRangeException.ThrowIfNegative(completedVehicleCount);

        Revenue = revenue;
        CompletedVehicleCount = completedVehicleCount;
        Note = note;
    }
}
