var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    string databasePath = builder.Configuration["ParkingBusinessResult:DatabasePath"]
        ?? Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "../../database/sqlite/parking_business_result.db"));
    builder.Services.AddSingleton<ParkingBusinessResult.Domain.IBusinessResultRepository>(
        new ParkingBusinessResult.Infrastructure.SqliteBusinessResultRepository(databasePath));
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    ParkingBusinessResult.Api.Development.BusinessResultEndpoints.Map(app);
}

app.Run();
