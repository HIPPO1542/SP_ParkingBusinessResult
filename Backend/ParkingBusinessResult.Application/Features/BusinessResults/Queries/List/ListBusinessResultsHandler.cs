using ParkingBusinessResult.Domain;

namespace ParkingBusinessResult.Application.Features.BusinessResults.Queries.List;

public sealed class ListBusinessResultsHandler(IBusinessResultRepository repository)
{
    public IReadOnlyList<ParkingBusinessResultRecord> Handle(ListBusinessResultsQuery query) =>
        repository.List().Where(result => query.Access.CanAccess(result.ParkingLotId)).ToArray();
}
