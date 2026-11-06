namespace ParkingBusinessResult.Application.Features.BusinessReports.Common;

public sealed record ReportActor(string Id, string? Name, string? Issuer);
public sealed record ReportDocument(byte[] Content, string ContentType, string Extension);
public sealed record ReportDownload(string Path, string ContentType, string FileName);
public sealed record SavedBusinessReport(Guid Id, string FileUrl, string FileName, string FileFormat, long FileSize,
    int RecordCount, string CreatedAt, string? CreatedBy, string CreatedById,
    string UpdatedAt, string? UpdatedBy, string UpdatedById);

public interface IBusinessReportDocumentWriter
{
    ReportDocument Write(string format, string title, string period, IReadOnlyList<BusinessReportRow> rows);
}

public interface IBusinessReportExportStore
{
    SavedBusinessReport Save(ReportActor actor, string format, ReportDocument document, int recordCount);
    ReportDownload? FindFile(ReportActor actor, string fileName);
}
