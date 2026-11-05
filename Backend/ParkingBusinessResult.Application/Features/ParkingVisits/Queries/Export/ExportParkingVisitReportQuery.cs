using ParkingBusinessResult.Application.Features.BusinessResults.Common;

namespace ParkingBusinessResult.Application.Features.ParkingVisits.Queries.Export;

public sealed record ExportParkingVisitReportQuery(DateTime Month, Guid? ParkingLotId, BusinessAccess Access, string? District);
