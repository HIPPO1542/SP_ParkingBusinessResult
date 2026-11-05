using ParkingBusinessResult.Application.Features.DevelopmentAccounts.Common;

namespace ParkingBusinessResult.Application.Features.DevelopmentAccounts.Commands.SignIn;

public sealed class SignInHandler
{
    private readonly IDevelopmentAccountRepository repository;
    private readonly IDevelopmentCredentials credentials;
    private readonly string dummyHash;

    public SignInHandler(IDevelopmentAccountRepository repository, IDevelopmentCredentials credentials)
    {
        this.repository = repository;
        this.credentials = credentials;
        dummyHash = credentials.Hash("dummy", Guid.NewGuid().ToString());
    }

    public string? Handle(SignInCommand command)
    {
        string username = command.Username ?? string.Empty;
        DevelopmentAccount? account = repository.FindByUsername(username);
        bool valid = credentials.Verify(username, account?.PasswordHash ?? dummyHash, command.Password ?? string.Empty);
        return account is null || !valid ? null : credentials.IssueToken(account);
    }
}
