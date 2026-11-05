using ParkingBusinessResult.Application.Features.BusinessReports.Common;

namespace ParkingBusinessResult.Application.Features.BusinessReports.Queries.List;

public sealed record BusinessReportListItem(
    Guid Id,
    string ReportingPeriod,
    ReportParkingLot ParkingLot,
    string? OperatorName,
    decimal Revenue,
    int VehicleCount,
    decimal AverageRevenuePerVehicle,
    string? CreatedAt,
    string? CreatedBy,
    string? CreatedById,
    string? UpdatedAt,
    string? UpdatedBy,
    string? UpdatedById);

public sealed record BusinessReportMetadata(
    ReportParkingLot ParkingLot,
    string? OperatorName,
    string? CreatedAt,
    string? CreatedBy,
    string? CreatedById,
    string? UpdatedAt,
    string? UpdatedBy,
    string? UpdatedById);

public interface IBusinessReportMetadataReader
{
    IReadOnlyDictionary<Guid, BusinessReportMetadata> FindMany(IReadOnlySet<Guid> resultIds);
}
