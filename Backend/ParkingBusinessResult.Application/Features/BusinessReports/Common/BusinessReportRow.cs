namespace ParkingBusinessResult.Application.Features.BusinessReports.Common;

public sealed record BusinessReportRow(Guid ParkingLotId, string ReportingPeriod, decimal Revenue, int CompletedVehicleCount);
