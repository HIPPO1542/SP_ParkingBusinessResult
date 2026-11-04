using ParkingBusinessResult.Domain;
using ParkingBusinessResult.Application.Features.BusinessReports;
using ParkingBusinessResult.Infrastructure;

Check(2_450_000_000m, 112_500, 21_777.78m);
Check(100m, 0, 0m);
Check(1m, 8, 0.13m);

BusinessSummary summary = BusinessSummaryCalculator.Calculate(
[
    new(1_000m, 2),
    new(2_000m, 4)
]);
if (summary != new BusinessSummary(3_000m, 6, 500m))
{
    throw new InvalidOperationException($"Unexpected summary: {summary}");
}

BusinessSummary emptySummary = BusinessSummaryCalculator.Calculate([]);
if (emptySummary != new BusinessSummary(0m, 0, 0m))
{
    throw new InvalidOperationException($"Unexpected empty summary: {emptySummary}");
}

static void Check(decimal revenue, long completedVehicles, decimal expected)
{
    decimal actual = RevenueMetrics.AverageRevenuePerVehicle(revenue, completedVehicles);
    if (actual != expected)
    {
        throw new InvalidOperationException($"Expected {expected}, got {actual}.");
    }
}

Console.WriteLine("Revenue metric checks passed.");

var businessRecord = new ParkingBusinessResultRecord(Guid.NewGuid(), Guid.NewGuid(), "2026-10", 100m, 2, null);
businessRecord.Update(300m, 3, "Updated");
if (businessRecord.Revenue != 300m || businessRecord.CompletedVehicleCount != 3 || businessRecord.Note != "Updated")
{
    throw new InvalidOperationException("Business result update failed.");
}

try
{
    businessRecord.Update(500m, -1, null);
    throw new InvalidOperationException("A negative vehicle count was accepted.");
}
catch (ArgumentOutOfRangeException)
{
    if (businessRecord.Revenue != 300m || businessRecord.CompletedVehicleCount != 3 || businessRecord.Note != "Updated")
    {
        throw new InvalidOperationException("Invalid update changed the businessRecord.");
    }
}

Console.WriteLine("Business result checks passed.");

var repository = new InMemoryBusinessResultRepository();
Guid parkingLotId = Guid.NewGuid();
var firstResult = new ParkingBusinessResultRecord(Guid.NewGuid(), parkingLotId, "2026-10", 100m, 2, null);
if (!repository.TryAdd(firstResult) || repository.TryAdd(firstResult) ||
    repository.TryAdd(new ParkingBusinessResultRecord(Guid.NewGuid(), parkingLotId, "2026-10", 200m, 1, null)))
{
    throw new InvalidOperationException("Adding a record or detecting duplicates failed.");
}

firstResult.Update(999m, 9, null);
ParkingBusinessResultRecord saved = repository.FindById(firstResult.Id)
    ?? throw new InvalidOperationException("Saved record was not found.");
if (saved.Revenue != 100m || repository.List().Count != 1)
{
    throw new InvalidOperationException("Repository state changed outside the repository.");
}

saved.Update(888m, 8, null);
if (repository.FindById(firstResult.Id)?.Revenue != 100m ||
    !repository.TryUpdate(firstResult.Id, 300m, 3, "Revised") ||
    repository.FindById(firstResult.Id)?.Revenue != 300m ||
    !repository.TryDelete(firstResult.Id) || repository.TryDelete(firstResult.Id) ||
    repository.FindById(firstResult.Id) is not null || repository.List().Count != 0)
{
    throw new InvalidOperationException("Repository update, delete, or copy isolation failed.");
}

Console.WriteLine("In-memory repository checks passed.");

