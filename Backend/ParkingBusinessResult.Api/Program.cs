var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    ParkingBusinessResult.Api.Extensions.DevelopmentSetup.Configure(builder);
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    ParkingBusinessResult.Api.Extensions.DevelopmentSetup.Map(app);
}

app.Run();
