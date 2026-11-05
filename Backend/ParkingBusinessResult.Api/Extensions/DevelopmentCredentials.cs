using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using ParkingBusinessResult.Application.Common;

namespace ParkingBusinessResult.Api.Extensions;

public sealed class DevelopmentCredentials(IConfiguration configuration) : IDevelopmentCredentials
{
    private readonly PasswordHasher<string> passwordHasher = new();

    public string Hash(string username, string password) => passwordHasher.HashPassword(username, password);

    public bool Verify(string username, string hash, string password) =>
        passwordHasher.VerifyHashedPassword(username, hash, password) != PasswordVerificationResult.Failed;

    public string IssueToken(DevelopmentAccount account)
    {
        var claims = new List<Claim> { new("sub", account.Id.ToString()), new("role", account.Role) };
        if (account.ParkingLotId is not null) claims.Add(new Claim("parking_lot_id", account.ParkingLotId.ToString()!));
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Authentication:SigningKey"]!));
        var token = new JwtSecurityToken(configuration["Authentication:Issuer"], configuration["Authentication:Audience"], claims,
            expires: DateTime.UtcNow.AddMinutes(30), signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
