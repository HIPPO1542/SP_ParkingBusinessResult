using ParkingBusinessResult.Application.Features.LocalAccounts.Common;

namespace ParkingBusinessResult.Application.Features.LocalAccounts.Commands.SignIn;

public sealed class SignInHandler
{
    private readonly ILocalAccountRepository repository;
    private readonly ILocalCredentials credentials;
    private readonly string dummyHash;

    public SignInHandler(ILocalAccountRepository repository, ILocalCredentials credentials)
    {
        this.repository = repository;
        this.credentials = credentials;
        dummyHash = credentials.Hash("dummy", Guid.NewGuid().ToString());
    }

    public string? Handle(SignInCommand command)
    {
        string username = command.Username ?? string.Empty;
        LocalAccount? account = repository.FindByUsername(username);
        bool valid = credentials.Verify(username, account?.PasswordHash ?? dummyHash, command.Password ?? string.Empty);
        return account is null || !valid ? null : credentials.IssueToken(account);
    }
}
