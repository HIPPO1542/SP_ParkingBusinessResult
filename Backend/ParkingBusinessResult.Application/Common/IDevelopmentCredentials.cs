namespace ParkingBusinessResult.Application.Common;

public interface IDevelopmentCredentials
{
    string Hash(string username, string password);
    bool Verify(string username, string hash, string password);
    string IssueToken(DevelopmentAccount account);
}
