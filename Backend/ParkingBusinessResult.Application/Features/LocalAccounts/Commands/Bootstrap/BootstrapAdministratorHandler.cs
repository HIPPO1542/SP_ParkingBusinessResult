using ParkingBusinessResult.Application.Features.LocalAccounts.Common;

namespace ParkingBusinessResult.Application.Features.LocalAccounts.Commands.Bootstrap;

public sealed class BootstrapAdministratorHandler(ILocalAccountRepository repository, ILocalCredentials credentials)
{
    public void Handle(BootstrapAdministratorCommand command)
    {
        repository.Initialize();
        if (string.IsNullOrEmpty(command.Username) && string.IsNullOrEmpty(command.Password)) return;
        if (string.IsNullOrWhiteSpace(command.Username) || command.Username.Length > 100 ||
            command.Password is null || command.Password.Length is < 12 or > 256)
            throw new InvalidOperationException("The bootstrap username and a password of at least 12 characters are required.");

        var account = new LocalAccount(Guid.NewGuid(), command.Username.Trim(),
            credentials.Hash(command.Username, command.Password), "ROLE_ADMIN", null);
        repository.AddFirstAdministrator(account);
    }
}
