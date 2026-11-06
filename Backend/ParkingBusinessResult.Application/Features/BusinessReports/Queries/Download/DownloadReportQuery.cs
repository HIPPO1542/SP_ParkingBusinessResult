using ParkingBusinessResult.Application.Features.BusinessReports.Common;

namespace ParkingBusinessResult.Application.Features.BusinessReports.Queries.Download;

public sealed record DownloadReportQuery(ReportActor Actor, string FileName);

public sealed class DownloadReportHandler(IReportExportStore store)
{
    public ReportDownload? Handle(DownloadReportQuery query) => store.FindFile(query.Actor, query.FileName);
}
