using ParkingBusinessResult.Application.Features.BusinessResults.Common;

namespace ParkingBusinessResult.Application.Features.ParkingVisits.Commands.Import;

public sealed record ImportParkingVisitsCommand(Stream Content, BusinessAccess Access);
