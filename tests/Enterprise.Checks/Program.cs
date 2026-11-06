using System.Collections;
using Microsoft.Data.Sqlite;
using ParkingBusinessResult.Application.Features.BusinessReports.Common;
using ParkingBusinessResult.Application.Features.BusinessResults.Common;
using ParkingBusinessResult.Application.Features.ParkingBookings.Commands.Import;
using ParkingBusinessResult.Application.Features.ParkingBookings.Common;
using ParkingBusinessResult.Application.Features.ParkingBookings.Queries.List;
using ParkingBusinessResult.Infrastructure.Reports;
using ParkingBusinessResult.Infrastructure.Repositories;

SQLitePCL.Batteries_V2.Init();
string root = args.Length > 0 ? Path.GetFullPath(args[0]) : Directory.GetCurrentDirectory();
string directory = Path.Combine(Path.GetTempPath(), "parking-enterprise-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(directory);
string path = Path.Combine(directory, "data.db");
Guid lot = Guid.NewGuid();
var access = new BusinessAccess(true, new HashSet<Guid>());

void Execute(string sql)
{
    using var connection = new SqliteConnection($"Data Source={path};Pooling=False");
    connection.Open();
    using SqliteCommand command = connection.CreateCommand();
    command.CommandText = sql;
    command.ExecuteNonQuery();
}

long Count()
{
    using var connection = new SqliteConnection($"Data Source={path};Pooling=False");
    connection.Open();
    using SqliteCommand command = connection.CreateCommand();
    command.CommandText = "SELECT COUNT(*) FROM SP_datTruocChoDoXe";
    return (long)command.ExecuteScalar()!;
}

ParkingBookingRow Row(string plate) => new(2, ["LOT", plate, "MOTORBIKE", "2026-11-05 08:00", "2026-11-05 09:00", "5000"]);
void Check(bool passed, string description) { if (!passed) throw new InvalidOperationException(description); }

try
{
    Execute("CREATE TABLE DL_KTHT_baiDoXe (Id TEXT PRIMARY KEY,ma TEXT,ten TEXT,diaChi TEXT);");
    using (var connection = new SqliteConnection($"Data Source={path};Pooling=False"))
    {
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "INSERT INTO DL_KTHT_baiDoXe VALUES ($id,'LOT','Bai mau','Quan mau')";
        command.Parameters.AddWithValue("$id", lot.ToString());
        command.ExecuteNonQuery();
    }
    Execute(File.ReadAllText(Path.Combine(root, "database/sqlite/007_booking_table.sql")));
    var repository = new SqliteParkingBookingImportRepository(path);
    var reader = new TestReader([Row("TX-1"), Row("FAIL")]);
    var handler = new ImportParkingBookingsHandler(reader, repository);
    Execute("CREATE TRIGGER force_import_failure BEFORE INSERT ON SP_datTruocChoDoXe WHEN NEW.bienSoXe='FAIL' BEGIN SELECT RAISE(ABORT,'storage_failure'); END;");
    bool failed = false;
    try { handler.Handle(new(Stream.Null, ".xlsx", access, "user-1", "admin")); }
    catch (SqliteException) { failed = true; }
    Check(failed && Count() == 0, "Storage failure must roll back the earlier valid row.");
    Execute("DROP TRIGGER force_import_failure;");

    var result = handler.Handle(new(Stream.Null, ".xlsx", access, "user-1", "admin"));
    Check(result.SuccessRows == 2 && Count() == 2, "Valid batch must commit.");
    using (var connection = new SqliteConnection($"Data Source={path};Pooling=False"))
    {
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM SP_datTruocChoDoXe WHERE idNguoiTao='user-1' AND nguoiTao='admin'";
        Check((long)command.ExecuteScalar()! == 2, "Created-by audit must be persisted.");
    }
    result = handler.Handle(new(Stream.Null, ".xlsx", access));
    Check(result.SuccessRows == 0 && result.DuplicateRows == 2 && Count() == 2, "Duplicates must never update existing rows.");

    using var cancellation = new CancellationTokenSource();
    var cancellingRows = new CancellingRows([Row("CT-1"), Row("CT-2")], cancellation);
    bool cancelled = false;
    try { new ImportParkingBookingsHandler(new TestReader(cancellingRows), repository).Handle(new(Stream.Null, ".xlsx", access), cancellation.Token); }
    catch (OperationCanceledException) { cancelled = true; }
    Check(cancelled && Count() == 2, "Cancelled batch must roll back its writes.");

    var queries = new SqliteParkingBookingQueryRepository(path);
    var page = queries.GetPage(new(null, null, "motorbike", 20, 1, access));
    Check(page.TotalCount == 2 && page.Items.Count == 0, "An empty page must retain the filtered total.");
    page = queries.GetPage(new(null, null, null, 1, 10, new(false, new HashSet<Guid>())));
    Check(page.TotalCount == 0 && page.Items.Count == 0, "No parking-lot permission must expose no rows.");

    var store = new BusinessReportExportStore(Path.Combine(directory, "exports"));
    var actor = new ReportActor("opaque-user-id", "admin", "https://issuer-one.example");
    var document = new ReportDocument([1, 2, 3], "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", ".xlsx");
    SavedBusinessReport saved = store.Save(actor, "EXCEL", document, 2);
    Check(store.FindFile(actor, saved.FileName) is not null, "Owner must be able to download the file.");
    Check(store.FindFile(actor with { Issuer = "https://issuer-two.example" }, saved.FileName) is null, "A subject from another issuer must not own this report.");
    Check(store.FindFile(actor, "../" + saved.FileName) is null, "Traversal must be rejected.");
    Check(!Directory.EnumerateFiles(directory, "*.tmp", SearchOption.AllDirectories).Any(), "Successful writes must leave no temporary report.");
    Console.WriteLine("Atomic rollback, cancellation, audit, SQL pagination, access isolation and report ownership passed.");
}
finally
{
    SqliteConnection.ClearAllPools();
    string resolved = Path.GetFullPath(directory);
    if (!resolved.StartsWith(Path.GetFullPath(Path.GetTempPath()), StringComparison.OrdinalIgnoreCase))
        throw new InvalidOperationException("Unexpected test directory.");
    Directory.Delete(resolved, recursive: true);
}

sealed class TestReader(IReadOnlyList<ParkingBookingRow> rows) : IParkingBookingSpreadsheetReader
{
    public IReadOnlyList<ParkingBookingRow> Read(Stream content, string extension) => rows;
}

sealed class CancellingRows(IReadOnlyList<ParkingBookingRow> rows, CancellationTokenSource cancellation) : IReadOnlyList<ParkingBookingRow>
{
    public int Count => rows.Count;
    public ParkingBookingRow this[int index] => rows[index];
    public IEnumerator<ParkingBookingRow> GetEnumerator()
    {
        for (int index = 0; index < rows.Count; index++)
        {
            if (index == 1) cancellation.Cancel();
            yield return rows[index];
        }
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
