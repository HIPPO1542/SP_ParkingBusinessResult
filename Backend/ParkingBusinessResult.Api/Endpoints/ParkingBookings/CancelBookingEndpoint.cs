using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using ParkingBusinessResult.Application.Features.ParkingBookings.Commands.Cancel;

namespace ParkingBusinessResult.Api.Endpoints.ParkingBookings;

public static class CancelBookingEndpoint
{
    public sealed record Request(string? Note)
    {
        [JsonExtensionData] public Dictionary<string, JsonElement>? ExtraFields { get; init; }
    }

    public static void Map(RouteGroupBuilder routes) => routes.MapPost("/{id:guid}/cancel", Handle);

    private static IResult Handle(Guid id, Request request, ClaimsPrincipal user, CancelBookingHandler handler) =>
        request.ExtraFields is { Count: > 0 } ? EndpointSupport.InvalidBody() :
            EndpointSupport.Respond(handler.Handle(new CancelBookingCommand(id, request.Note, user.IsInRole("ROLE_CUSTOMER"),
                EndpointSupport.GetAccess(user), EndpointSupport.GetAuditActor(user))));
}
