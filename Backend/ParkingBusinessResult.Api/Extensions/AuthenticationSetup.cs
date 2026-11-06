using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ParkingBusinessResult.Api.Contracts;

namespace ParkingBusinessResult.Api.Extensions;

public static class AuthenticationSetup
{
    public static void Configure(WebApplicationBuilder builder)
    {
        string? issuer = builder.Configuration["Authentication:Issuer"];
        string? audience = builder.Configuration["Authentication:Audience"];
        string? signingKey = builder.Configuration["Authentication:SigningKey"];
        string mode = builder.Configuration["Authentication:Mode"] ?? "Local";
        if (mode is not ("Local" or "Authority"))
            throw new InvalidOperationException("Authentication:Mode must be Local or Authority.");
        bool localAuthentication = mode == "Local";
        string? authority = builder.Configuration["Authentication:Authority"];
        if (string.IsNullOrWhiteSpace(audience))
            throw new InvalidOperationException("Authentication:Audience is required.");
        if (localAuthentication && (string.IsNullOrWhiteSpace(issuer) ||
            string.IsNullOrWhiteSpace(signingKey) || Encoding.UTF8.GetByteCount(signingKey) < 32))
        {
            throw new InvalidOperationException("Local authentication requires an issuer, audience and a signing key of at least 32 bytes.");
        }
        if (!localAuthentication && (!Uri.TryCreate(authority, UriKind.Absolute, out Uri? provider) || provider.Scheme != Uri.UriSchemeHttps))
            throw new InvalidOperationException("Authority authentication requires an HTTPS identity provider.");

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    ValidateIssuerSigningKey = true,
                    RoleClaimType = builder.Configuration["Authentication:RoleClaimType"] ?? "role",
                    NameClaimType = "sub",
                    ClockSkew = TimeSpan.Zero
                };
                if (localAuthentication)
                {
                    options.TokenValidationParameters.ValidIssuer = issuer;
                    options.TokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey!));
                    options.TokenValidationParameters.ValidAlgorithms = [SecurityAlgorithms.HmacSha256];
                }
                else
                {
                    options.Authority = authority;
                    options.RequireHttpsMetadata = true;
                }
                options.Events = new JwtBearerEvents
                {
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = StatusCodes.Status200OK;
                        await context.Response.WriteAsJsonAsync(ApiResponse<object>.BusinessError("Vui lòng đăng nhập."));
                    },
                    OnForbidden = async context =>
                    {
                        context.Response.StatusCode = StatusCodes.Status200OK;
                        await context.Response.WriteAsJsonAsync(ApiResponse<object>.BusinessError("Bạn không có quyền thực hiện thao tác này."));
                    }
                };
            });
        builder.Services.AddAuthorization(options => options.AddPolicy("BusinessResultAccess", policy =>
            policy.RequireAuthenticatedUser()
                .RequireAssertion(context => !string.IsNullOrWhiteSpace(context.User.FindFirst("sub")?.Value))
                .RequireRole("ROLE_ADMIN", "ROLE_DISTRICT_ADMIN", "ROLE_PARKING_OPERATOR")));
        builder.Services.AddAuthorization(options => options.AddPolicy("LocalAccountAdmin", policy =>
            policy.RequireAuthenticatedUser()
                .RequireAssertion(context => !string.IsNullOrWhiteSpace(context.User.FindFirst("sub")?.Value))
                .RequireRole("ROLE_ADMIN")));
        builder.Services.AddRateLimiter(options =>
        {
            options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status200OK;
                await context.HttpContext.Response.WriteAsJsonAsync(
                    ApiResponse<object>.BusinessError("Quá nhiều lần đăng nhập, vui lòng thử lại sau."), cancellationToken);
            };
        });
    }
}
