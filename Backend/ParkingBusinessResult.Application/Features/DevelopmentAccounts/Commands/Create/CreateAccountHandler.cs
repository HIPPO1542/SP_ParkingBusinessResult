using ParkingBusinessResult.Application.Features.DevelopmentAccounts.Common;

namespace ParkingBusinessResult.Application.Features.DevelopmentAccounts.Commands.Create;

public sealed class CreateAccountHandler(IDevelopmentAccountRepository repository, IDevelopmentCredentials credentials)
{
    public bool Handle(CreateAccountCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Username) || command.Username.Length > 100 ||
            command.Password is null || command.Password.Length is < 12 or > 256 ||
            command.Role is not ("ROLE_ADMIN" or "ROLE_DISTRICT_ADMIN" or "ROLE_PARKING_OPERATOR") ||
            (command.Role != "ROLE_ADMIN" && (command.ParkingLotId is null || command.ParkingLotId == Guid.Empty)))
            return false;

        var account = new DevelopmentAccount(Guid.NewGuid(), command.Username.Trim(),
            credentials.Hash(command.Username, command.Password), command.Role, command.ParkingLotId);
        return repository.TryAdd(account);
    }
}
