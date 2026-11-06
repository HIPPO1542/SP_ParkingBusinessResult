namespace ParkingBusinessResult.Application.Features.LocalAccounts.Common;

public interface ILocalCredentials
{
    string Hash(string username, string password);
    bool Verify(string username, string hash, string password);
    string IssueToken(LocalAccount account);
}
