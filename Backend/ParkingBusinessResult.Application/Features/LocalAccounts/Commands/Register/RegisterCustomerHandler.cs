using ParkingBusinessResult.Application.Features.LocalAccounts.Common;

namespace ParkingBusinessResult.Application.Features.LocalAccounts.Commands.Register;

public sealed record RegisteredCustomer(Guid Id, string Username, string Role);

public sealed class RegisterCustomerHandler(ILocalAccountRepository repository, ILocalCredentials credentials)
{
    public RegisteredCustomer? Handle(RegisterCustomerCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Username) || command.Username.Length > 100 ||
            command.Password is null || command.Password.Length is < 12 or > 256)
            return null;

        string username = command.Username.Trim();
        var account = new LocalAccount(Guid.NewGuid(), username, credentials.Hash(username, command.Password), "ROLE_CUSTOMER", null);
        return repository.TryAdd(account) ? new(account.Id, account.Username, account.Role) : null;
    }
}
