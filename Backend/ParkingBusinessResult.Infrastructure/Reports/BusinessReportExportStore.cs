using Microsoft.Data.Sqlite;

namespace ParkingBusinessResult.Infrastructure.Reports;

public sealed class BusinessReportExportStore(string databasePath, string exportDirectory)
{
    public string? FindUsername(Guid userId)
    {
        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadOnly
        }.ToString());
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT username FROM dev_auth_users WHERE id = $id";
        command.Parameters.AddWithValue("$id", userId.ToString());
        return command.ExecuteScalar() as string;
    }

    public void Save(Guid userId, string fileName, byte[] content)
    {
        string directory = Path.Combine(exportDirectory, userId.ToString("N"));
        Directory.CreateDirectory(directory);
        using var file = new FileStream(Path.Combine(directory, fileName), FileMode.CreateNew, FileAccess.Write, FileShare.None);
        file.Write(content);
    }

    public string? FindFile(Guid userId, string fileName)
    {
        if (fileName != Path.GetFileName(fileName) || !fileName.StartsWith("BaoCao_KetQuaKinhDoanh_", StringComparison.Ordinal) ||
            !(fileName.EndsWith(".xlsx", StringComparison.Ordinal) || fileName.EndsWith(".pdf", StringComparison.Ordinal)))
            return null;
        string path = Path.Combine(exportDirectory, userId.ToString("N"), fileName);
        return File.Exists(path) ? path : null;
    }
}
