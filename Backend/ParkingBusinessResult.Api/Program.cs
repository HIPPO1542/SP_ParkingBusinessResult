var builder = WebApplication.CreateBuilder(args);

// Opt-in for a single-machine installation; deployed hosts can use environment/secret providers instead.
if (builder.Configuration.GetValue<bool>("Authentication:UseUserSecrets"))
    builder.Configuration.AddUserSecrets(typeof(ParkingBusinessResult.Api.Extensions.ApplicationSetup).Assembly, optional: true)
        .AddEnvironmentVariables().AddCommandLine(args);

ParkingBusinessResult.Api.Extensions.ApplicationSetup.Configure(builder);

var app = builder.Build();

ParkingBusinessResult.Api.Extensions.ApplicationSetup.Map(app);

app.Run();
