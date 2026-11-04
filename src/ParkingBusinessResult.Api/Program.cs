var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    ParkingBusinessResult.Api.Development.DevelopmentAuthentication.Configure(builder);
    string databasePath = builder.Configuration["ParkingBusinessResult:DatabasePath"]
        ?? Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "../../database/sqlite/parking_business_result.db"));
    builder.Services.AddSingleton<ParkingBusinessResult.Domain.IBusinessResultRepository>(
        new ParkingBusinessResult.Infrastructure.SqliteBusinessResultRepository(databasePath));
    builder.Services.AddSingleton(new ParkingBusinessResult.Api.Development.DevelopmentAccounts(databasePath, builder.Configuration));
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseRateLimiter();
    app.Services.GetRequiredService<ParkingBusinessResult.Api.Development.DevelopmentAccounts>().Initialize();
    ParkingBusinessResult.Api.Development.DevelopmentAccounts.Map(app);
    ParkingBusinessResult.Api.Development.BusinessResultEndpoints.Map(app);
}

app.Run();
