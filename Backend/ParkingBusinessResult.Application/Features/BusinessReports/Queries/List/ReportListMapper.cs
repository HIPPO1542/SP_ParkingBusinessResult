using ParkingBusinessResult.Domain;

namespace ParkingBusinessResult.Application.Features.BusinessReports.Queries.List;

public sealed class ReportListMapper(IReportMetadataReader metadataReader)
{
    public IReadOnlyList<ReportListItem> Map(IReadOnlyList<BusinessResultRecord> records)
    {
        IReadOnlyDictionary<Guid, BusinessReportMetadata> metadata = metadataReader.FindMany(records.Select(record => record.Id).ToHashSet());
        return records.Select(record =>
        {
            BusinessReportMetadata data = metadata[record.Id];
            return new ReportListItem(record.Id, record.ReportingPeriod, data.ParkingLot, data.OperatorName,
                record.Revenue, record.CompletedVehicleCount,
                RevenueMetrics.AverageRevenuePerVehicle(record.Revenue, record.CompletedVehicleCount),
                data.CreatedAt, data.CreatedBy, data.CreatedById, data.UpdatedAt, data.UpdatedBy, data.UpdatedById);
        }).ToArray();
    }
}
