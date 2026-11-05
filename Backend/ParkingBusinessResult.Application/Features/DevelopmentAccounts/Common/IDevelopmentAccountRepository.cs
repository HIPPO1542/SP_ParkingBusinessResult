namespace ParkingBusinessResult.Application.Features.DevelopmentAccounts.Common;

public interface IDevelopmentAccountRepository
{
    void Initialize();
    void AddFirstAdministrator(DevelopmentAccount account);
    bool TryAdd(DevelopmentAccount account);
    DevelopmentAccount? FindByUsername(string username);
}
