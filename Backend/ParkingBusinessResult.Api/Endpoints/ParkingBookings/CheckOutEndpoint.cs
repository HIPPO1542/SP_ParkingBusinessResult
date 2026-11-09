using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using ParkingBusinessResult.Application.Features.ParkingBookings.Commands.CheckOut;

namespace ParkingBusinessResult.Api.Endpoints.ParkingBookings;

public static class CheckOutEndpoint
{
    public sealed record Request(string? DayType)
    {
        [JsonExtensionData] public Dictionary<string, JsonElement>? ExtraFields { get; init; }
    }

    public static void Map(RouteGroupBuilder routes) => routes.MapPost("/{id:guid}/check-out", Handle);

    private static IResult Handle(Guid id, Request request, ClaimsPrincipal user, CheckOutHandler handler) =>
        request.ExtraFields is { Count: > 0 } ? EndpointSupport.InvalidBody() :
            EndpointSupport.Respond(handler.Handle(new CheckOutCommand(id, request.DayType, EndpointSupport.GetAccess(user), EndpointSupport.GetAuditActor(user))));
}
