using ParkingBusinessResult.Application.Features.BusinessResults.Common;

namespace ParkingBusinessResult.Application.Features.ParkingVisits.Queries.CompareTariff;

public sealed record CompareParkingVisitTariffQuery(Guid VisitId, string DayType, BusinessAccess Access);
