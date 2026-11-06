using ParkingBusinessResult.Application.Features.BusinessReports.Common;

namespace ParkingBusinessResult.Application.Features.BusinessReports.Queries.Download;

public sealed record DownloadBusinessReportQuery(ReportActor Actor, string FileName);

public sealed class DownloadBusinessReportHandler(IBusinessReportExportStore store)
{
    public ReportDownload? Handle(DownloadBusinessReportQuery query) => store.FindFile(query.Actor, query.FileName);
}
