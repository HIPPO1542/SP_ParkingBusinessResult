using ParkingBusinessResult.Application.Features.BusinessResults.Commands.Create;
using ParkingBusinessResult.Application.Features.BusinessResults.Commands.Delete;
using ParkingBusinessResult.Application.Features.BusinessResults.Commands.Update;
using ParkingBusinessResult.Application.Features.BusinessResults.Queries.GetById;
using ParkingBusinessResult.Application.Features.BusinessResults.Queries.List;
using ParkingBusinessResult.Application.Features.DevelopmentAccounts.Commands.Bootstrap;
using ParkingBusinessResult.Application.Features.DevelopmentAccounts.Commands.Create;
using ParkingBusinessResult.Application.Features.DevelopmentAccounts.Commands.SignIn;

namespace ParkingBusinessResult.Api.Extensions;

public static class BusinessResultRegistration
{
    public static IServiceCollection AddBusinessResultHandlers(this IServiceCollection services)
    {
        services.AddScoped<CreateBusinessResultHandler>();
        services.AddScoped<UpdateBusinessResultHandler>();
        services.AddScoped<DeleteBusinessResultHandler>();
        services.AddScoped<GetBusinessResultHandler>();
        services.AddScoped<ListBusinessResultsHandler>();
        services.AddSingleton<BootstrapAdministratorHandler>();
        services.AddSingleton<CreateAccountHandler>();
        services.AddSingleton<SignInHandler>();
        return services;
    }
}
