using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using ParkingBusinessResult.Application.Features.BusinessReports.Common;

namespace ParkingBusinessResult.Infrastructure.Reports;

public sealed class ReportExportStore(string exportDirectory, bool allowLegacyDownloads = false) : IReportExportStore
{
    public SavedBusinessReport Save(ReportActor actor, string format, ReportDocument document, int recordCount)
    {
        Guid id = Guid.NewGuid();
        DateTimeOffset now = DateTimeOffset.Now;
        string fileName = $"BaoCao_KetQuaKinhDoanh_{now:yyyyMMdd_HHmmss}_{id:N}{document.Extension}";
        string directory = OwnerDirectory(actor);
        Directory.CreateDirectory(directory);
        string temporaryPath = Path.Combine(directory, fileName + ".tmp");
        string finalPath = Path.Combine(directory, fileName);
        try
        {
            using (var file = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                file.Write(document.Content);
                file.Flush(flushToDisk: true);
            }
            File.Move(temporaryPath, finalPath);
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
        string date = now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return new(id, $"/static/exports/{fileName}", fileName, format, document.Content.LongLength, recordCount,
            date, actor.Name, actor.Id, date, actor.Name, actor.Id);
    }

    public ReportDownload? FindFile(ReportActor actor, string fileName)
    {
        if (fileName.Length > 180 || fileName.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not ('_' or '.')) ||
            fileName != Path.GetFileName(fileName) || !fileName.StartsWith("BaoCao_KetQuaKinhDoanh_", StringComparison.Ordinal) ||
            !(fileName.EndsWith(".xlsx", StringComparison.Ordinal) || fileName.EndsWith(".pdf", StringComparison.Ordinal)))
            return null;
        string path = Path.Combine(OwnerDirectory(actor), fileName);
        if (!File.Exists(path) && allowLegacyDownloads && Guid.TryParse(actor.Id, out Guid legacyUserId))
            path = Path.Combine(exportDirectory, legacyUserId.ToString("N"), fileName);
        if (!File.Exists(path)) return null;
        string contentType = fileName.EndsWith(".pdf", StringComparison.Ordinal) ? "application/pdf"
            : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        return new(path, contentType, fileName);
    }

    private string OwnerDirectory(ReportActor actor) => Path.Combine(exportDirectory,
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{actor.Issuer}\0{actor.Id}"))));
}
