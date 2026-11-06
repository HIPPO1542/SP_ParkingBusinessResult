namespace ParkingBusinessResult.Application.Features.BusinessReports.Common;

public sealed record ReportRow(Guid ParkingLotId, string ReportingPeriod, decimal Revenue, int CompletedVehicleCount,
    string? ParkingLotCode = null, string? ParkingLotName = null);
