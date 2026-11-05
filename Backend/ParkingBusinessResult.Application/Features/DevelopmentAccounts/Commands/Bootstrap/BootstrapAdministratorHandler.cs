using ParkingBusinessResult.Application.Features.DevelopmentAccounts.Common;

namespace ParkingBusinessResult.Application.Features.DevelopmentAccounts.Commands.Bootstrap;

public sealed class BootstrapAdministratorHandler(IDevelopmentAccountRepository repository, IDevelopmentCredentials credentials)
{
    public void Handle(BootstrapAdministratorCommand command)
    {
        repository.Initialize();
        if (string.IsNullOrEmpty(command.Username) && string.IsNullOrEmpty(command.Password)) return;
        if (string.IsNullOrWhiteSpace(command.Username) || command.Username.Length > 100 ||
            command.Password is null || command.Password.Length is < 12 or > 256)
            throw new InvalidOperationException("The bootstrap username and a password of at least 12 characters are required.");

        var account = new DevelopmentAccount(Guid.NewGuid(), command.Username.Trim(),
            credentials.Hash(command.Username, command.Password), "ROLE_ADMIN", null);
        repository.AddFirstAdministrator(account);
    }
}
