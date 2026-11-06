using ParkingBusinessResult.Checks;
using ParkingBusinessResult.Infrastructure.Persistence;
using ParkingBusinessResult.Domain;
using ParkingBusinessResult.Application.Features.BusinessReports.Common;
using ParkingBusinessResult.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

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
    !repository.TrySoftDelete(firstResult.Id) || repository.TrySoftDelete(firstResult.Id) ||
    repository.FindById(firstResult.Id) is not null || repository.List().Count != 0)
{
    throw new InvalidOperationException("Repository update, delete, or copy isolation failed.");
}

if (repository.TryUpdate(firstResult.Id, 1m, 1, null) ||
    !repository.TryAdd(new ParkingBusinessResultRecord(Guid.NewGuid(), parkingLotId, "2026-10", 100m, 2, null)))
{
    throw new InvalidOperationException("A deleted record was updated or its period was not reusable.");
}

Console.WriteLine("In-memory repository checks passed.");

string databasePath = Path.Combine(Path.GetTempPath(), $"parking-business-{Guid.NewGuid():N}.db");
Guid testLotId = Guid.NewGuid();
try
{
    SQLitePCL.Batteries_V2.Init();
    using (var db = new ParkingContextFactory(databasePath).CreateDbContext())
    {
        string initial = db.Database.GetMigrations().Single(id => id.EndsWith("_InitialParkingSchema", StringComparison.Ordinal));
        db.GetService<IMigrator>().Migrate(initial);
    }
    using (var connection = new SqliteConnection($"Data Source={databasePath}"))
    {
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO "SP_donViKinhDoanhQuanLy" (id, tenDonVi, diaChi, nguoiLienHe) VALUES ('unit', 'Unit', 'Address', 'Contact');
            INSERT INTO "DL_KTHT_baiDoXe" (Id, quyHoachId, donViQuanLyId, geo, dienTichThucTe) VALUES ($lot, 'plan', 'unit', '{}', 1);
            """;
        command.Parameters.AddWithValue("$lot", testLotId.ToString());
        command.ExecuteNonQuery();
    }

    var sqliteRepository = new SqliteBusinessResultRepository(new ParkingContextFactory(databasePath));
    var persistentResult = new ParkingBusinessResultRecord(Guid.NewGuid(), testLotId, "2026-10", 123.45m, 4, "Test");
    if (!sqliteRepository.TryAdd(persistentResult, new("creator", "Creator")) || sqliteRepository.TryAdd(persistentResult))
    {
        throw new InvalidOperationException("SQLite insert or duplicate detection failed.");
    }

    var reopenedRepository = new SqliteBusinessResultRepository(new ParkingContextFactory(databasePath));
    if (reopenedRepository.FindById(persistentResult.Id)?.Revenue != 123.45m ||
        reopenedRepository.List().Count != 1 ||
        !reopenedRepository.TryUpdate(persistentResult.Id, 500m, 5, null, new("editor", "Editor")) ||
        reopenedRepository.FindById(persistentResult.Id)?.CompletedVehicleCount != 5 ||
        !reopenedRepository.TrySoftDelete(persistentResult.Id, new("deleter", "Deleter")) ||
        reopenedRepository.FindById(persistentResult.Id) is not null)
    {
        throw new InvalidOperationException("SQLite persistence or CRUD operations failed.");
    }

    if (reopenedRepository.TrySoftDelete(persistentResult.Id) ||
        reopenedRepository.TryUpdate(persistentResult.Id, 1m, 1, null) || reopenedRepository.List().Count != 0)
    {
        throw new InvalidOperationException("A soft-deleted SQLite result remained active.");
    }

    using (var connection = new SqliteConnection($"Data Source={databasePath}"))
    {
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT daXoa, doanhThu, soLuotXe, idNguoiTao, nguoiTao, idNguoiCapNhat, nguoiCapNhat, ngayTao, ngayCapNhat FROM SP_ketQuaKinhDoanhBaiDoXe WHERE id = $id";
        command.Parameters.AddWithValue("$id", persistentResult.Id.ToString());
        using SqliteDataReader reader = command.ExecuteReader();
        if (!reader.Read() || reader.GetInt32(0) != 1 || reader.GetDouble(1) != 500 || reader.GetInt32(2) != 5)
            throw new InvalidOperationException("Soft delete did not preserve the original SQLite record.");
        if (reader.GetString(3) != "creator" || reader.GetString(4) != "Creator" || reader.GetString(5) != "deleter" ||
            reader.GetString(6) != "Deleter" || reader.IsDBNull(7) || reader.IsDBNull(8))
            throw new InvalidOperationException("Mutation audit did not preserve the creator and record the latest actor.");
    }

    if (!reopenedRepository.TryAdd(new ParkingBusinessResultRecord(Guid.NewGuid(), testLotId, "2026-10", 200m, 2, null)))
        throw new InvalidOperationException("The period of a soft-deleted SQLite record could not be reused.");

    using (var db = new ParkingContextFactory(databasePath).CreateDbContext())
    {
        // Upgrade with an active and a deleted row sharing the same lot/period: an unfiltered index would fail.
        db.Database.Migrate();
        if (db.SpKetQuaKinhDoanhBaiDoXe.Count() != 2 || reopenedRepository.List().Count != 1 || db.Database.HasPendingModelChanges())
            throw new InvalidOperationException("Index alignment changed saved records or left the EF model inconsistent.");
    }

    try
    {
        sqliteRepository.TryAdd(new ParkingBusinessResultRecord(Guid.NewGuid(), Guid.NewGuid(), "2026-10", 1m, 1, null));
        throw new InvalidOperationException("A result for an unknown parking lot was accepted.");
    }
    catch (ParkingLotNotFoundException)
    {
        // The foreign key must reject results whose parking lot does not exist.
    }

    Console.WriteLine("SQLite repository checks passed.");
}
finally
{
    SqliteConnection.ClearAllPools();
    if (File.Exists(databasePath))
    {
        File.Delete(databasePath);
    }
}

