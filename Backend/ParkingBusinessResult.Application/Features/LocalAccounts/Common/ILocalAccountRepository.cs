namespace ParkingBusinessResult.Application.Features.LocalAccounts.Common;

public interface ILocalAccountRepository
{
    void Initialize();
    void AddFirstAdministrator(LocalAccount account);
    bool TryAdd(LocalAccount account);
    LocalAccount? FindByUsername(string username);
}
