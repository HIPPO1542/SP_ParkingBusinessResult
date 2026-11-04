using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.IdentityModel.Tokens;
using ParkingBusinessResult.Api.Contracts;

namespace ParkingBusinessResult.Api.Development;

public sealed class DevelopmentAccounts
{
    private readonly string connectionString;
    private readonly PasswordHasher<string> passwordHasher = new();
    private readonly IConfiguration configuration;
    private readonly string dummyHash;

    public DevelopmentAccounts(string databasePath, IConfiguration configuration)
    {
        this.configuration = configuration;
        connectionString = new SqliteConnectionStringBuilder { DataSource = databasePath, Mode = SqliteOpenMode.ReadWrite }.ToString();
        dummyHash = passwordHasher.HashPassword("dummy", Guid.NewGuid().ToString());
    }

    public void Initialize()
    {
        using SqliteConnection connection = Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS "dev_auth_users" (
                "id" TEXT NOT NULL PRIMARY KEY,
                "username" TEXT NOT NULL COLLATE NOCASE UNIQUE,
                "passwordHash" TEXT NOT NULL,
                "role" TEXT NOT NULL,
                "parkingLotId" TEXT
            )
            """;
        command.ExecuteNonQuery();

        string? username = configuration["Authentication:BootstrapUsername"];
        string? password = configuration["Authentication:BootstrapPassword"];
        if (string.IsNullOrEmpty(username) && string.IsNullOrEmpty(password)) return;
        if (string.IsNullOrWhiteSpace(username) || username.Length > 100 || password is null || password.Length is < 12 or > 256)
            throw new InvalidOperationException("The bootstrap username and a password of at least 12 characters are required.");

        using SqliteCommand bootstrap = connection.CreateCommand();
        bootstrap.CommandText = """
            INSERT INTO dev_auth_users (id, username, passwordHash, role)
            SELECT $id, $username, $hash, 'ROLE_ADMIN'
            WHERE NOT EXISTS (SELECT 1 FROM dev_auth_users)
            """;
        bootstrap.Parameters.AddWithValue("$id", Guid.NewGuid().ToString());
        bootstrap.Parameters.AddWithValue("$username", username.Trim());
        bootstrap.Parameters.AddWithValue("$hash", passwordHasher.HashPassword(username, password));
        bootstrap.ExecuteNonQuery();
    }

    public bool CreateUser(string username, string password, string role, Guid? parkingLotId)
    {
        if (string.IsNullOrWhiteSpace(username) || username.Length > 100 || password is null || password.Length < 12 || password.Length > 256 ||
            role is not ("ROLE_ADMIN" or "ROLE_DISTRICT_ADMIN" or "ROLE_PARKING_OPERATOR") ||
            (role != "ROLE_ADMIN" && (parkingLotId is null || parkingLotId == Guid.Empty))) return false;

        using SqliteConnection connection = Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "INSERT INTO dev_auth_users (id, username, passwordHash, role, parkingLotId) VALUES ($id, $username, $hash, $role, $lot)";
        command.Parameters.AddWithValue("$id", Guid.NewGuid().ToString());
        command.Parameters.AddWithValue("$username", username.Trim());
        command.Parameters.AddWithValue("$hash", passwordHasher.HashPassword(username, password));
        command.Parameters.AddWithValue("$role", role);
        command.Parameters.AddWithValue("$lot", (object?)parkingLotId?.ToString() ?? DBNull.Value);
        try { command.ExecuteNonQuery(); return true; }
        catch (SqliteException exception) when (exception.SqliteExtendedErrorCode is 1555 or 2067) { return false; }
    }

    public string? SignIn(string username, string password)
    {
        using SqliteConnection connection = Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT id, passwordHash, role, parkingLotId FROM dev_auth_users WHERE username = $username";
        command.Parameters.AddWithValue("$username", username ?? string.Empty);
        using SqliteDataReader reader = command.ExecuteReader();
        bool found = reader.Read();
        string hash = found ? reader.GetString(1) : dummyHash;
        PasswordVerificationResult verified = passwordHasher.VerifyHashedPassword(username ?? string.Empty, hash, password ?? string.Empty);
        if (!found || verified == PasswordVerificationResult.Failed) return null;

        var claims = new List<Claim> { new("sub", reader.GetString(0)), new("role", reader.GetString(2)) };
        if (!reader.IsDBNull(3)) claims.Add(new Claim("parking_lot_id", reader.GetString(3)));
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Authentication:SigningKey"]!));
        var token = new JwtSecurityToken(configuration["Authentication:Issuer"], configuration["Authentication:Audience"], claims,
            expires: DateTime.UtcNow.AddMinutes(30), signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private SqliteConnection Open()
    {
        var connection = new SqliteConnection(connectionString);
        connection.Open();
        return connection;
    }

    public static void Map(WebApplication app)
    {
        app.MapPost("/dev/auth/login", (LoginRequest request, DevelopmentAccounts accounts) =>
        {
            string? token = accounts.SignIn(request.Username, request.Password);
            return token is null
                ? Results.Ok(ApiResponse<object>.BusinessError("Tên đăng nhập hoặc mật khẩu không đúng."))
                : Results.Ok(ApiResponse<object>.Success(new { accessToken = token, expiresIn = 1800 }));
        }).RequireRateLimiting("login");

        app.MapPost("/dev/auth/users", (CreateAccountRequest request, DevelopmentAccounts accounts) =>
            accounts.CreateUser(request.Username, request.Password, request.Role, request.ParkingLotId)
                ? Results.Ok(ApiResponse<object>.Success(new { username = request.Username }))
                : Results.Ok(ApiResponse<object>.BusinessError("Thông tin tài khoản không hợp lệ hoặc tên đăng nhập đã tồn tại.")))
            .RequireAuthorization(policy => policy.RequireAuthenticatedUser()
                .RequireAssertion(context => !string.IsNullOrWhiteSpace(context.User.FindFirst("sub")?.Value))
                .RequireRole("ROLE_ADMIN"));
    }
}

public sealed record LoginRequest(string Username, string Password);
public sealed record CreateAccountRequest(string Username, string Password, string Role, Guid? ParkingLotId);
