using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ParkingBusinessResult.Infrastructure.Persistence;
using ParkingBusinessResult.Infrastructure.Persistence.Models;
using ParkingBusinessResult.Application.Features.LocalAccounts.Common;
namespace ParkingBusinessResult.Infrastructure.Repositories;
public sealed class LocalAccountRepository(ParkingDbContext db) : ILocalAccountRepository
{
    public void Initialize()
    {
        db.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS dev_auth_users (id TEXT NOT NULL PRIMARY KEY,username TEXT NOT NULL COLLATE NOCASE UNIQUE,passwordHash TEXT NOT NULL,role TEXT NOT NULL,parkingLotId TEXT)");
    }
    public void AddFirstAdministrator(LocalAccount account)
    {
        db.Database.ExecuteSqlInterpolated($"INSERT INTO dev_auth_users (id,username,passwordHash,role) SELECT {account.Id.ToString()},{account.Username},{account.PasswordHash},{account.Role} WHERE NOT EXISTS (SELECT 1 FROM dev_auth_users)");
    }
    public bool TryAdd(LocalAccount account)
    {
        db.DevAuthUsers.Add(new DevAuthUsers { Id = account.Id.ToString(), Username = account.Username, PasswordHash = account.PasswordHash, Role = account.Role, ParkingLotId = account.ParkingLotId?.ToString() });
        try { db.SaveChanges(); return true; }
        catch (DbUpdateException error) when (error.InnerException is SqliteException { SqliteExtendedErrorCode: 1555 or 2067 }) { return false; }
    }
    public LocalAccount? FindByUsername(string username)
    {
        var row = db.DevAuthUsers.AsNoTracking().SingleOrDefault(row => row.Username == username);
        return row is null ? null : new(Guid.Parse(row.Id), row.Username, row.PasswordHash, row.Role, row.ParkingLotId is null ? null : Guid.Parse(row.ParkingLotId));
    }
}
