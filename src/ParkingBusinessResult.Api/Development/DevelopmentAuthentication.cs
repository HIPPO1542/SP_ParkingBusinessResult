using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ParkingBusinessResult.Api.Contracts;

namespace ParkingBusinessResult.Api.Development;

public static class DevelopmentAuthentication
{
    public static void Configure(WebApplicationBuilder builder)
    {
        string? issuer = builder.Configuration["Authentication:Issuer"];
        string? audience = builder.Configuration["Authentication:Audience"];
        string? signingKey = builder.Configuration["Authentication:SigningKey"];
        if (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(audience) ||
            string.IsNullOrWhiteSpace(signingKey) || Encoding.UTF8.GetByteCount(signingKey) < 32)
        {
            throw new InvalidOperationException("Development authentication requires an issuer, audience and a signing key of at least 32 bytes.");
        }

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    RoleClaimType = "role",
                    NameClaimType = "sub",
                    ClockSkew = TimeSpan.Zero
                };
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
