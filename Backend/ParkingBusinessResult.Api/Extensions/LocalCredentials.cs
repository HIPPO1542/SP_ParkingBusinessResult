using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using ParkingBusinessResult.Application.Features.LocalAccounts.Common;

namespace ParkingBusinessResult.Api.Extensions;

public sealed class LocalCredentials(IConfiguration configuration) : ILocalCredentials
{
    private readonly PasswordHasher<string> passwordHasher = new();

    public string Hash(string username, string password) => passwordHasher.HashPassword(username, password);

    public bool Verify(string username, string hash, string password) =>
        passwordHasher.VerifyHashedPassword(username, hash, password) != PasswordVerificationResult.Failed;

    public string IssueToken(LocalAccount account)
    {
        var claims = new List<Claim> { new("sub", account.Id.ToString()), new("role", account.Role), new("unique_name", account.Username) };
        if (account.ParkingLotId is not null) claims.Add(new Claim("parking_lot_id", account.ParkingLotId.ToString()!));
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Authentication:SigningKey"]!));
        var token = new JwtSecurityToken(configuration["Authentication:Issuer"], configuration["Authentication:Audience"], claims,
            expires: DateTime.UtcNow.AddMinutes(30), signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
