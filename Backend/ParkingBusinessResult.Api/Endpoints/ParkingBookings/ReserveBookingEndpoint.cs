using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using ParkingBusinessResult.Api.Contracts;
using ParkingBusinessResult.Application.Features.ParkingBookings.Commands.Reserve;
using ParkingBusinessResult.Application.Features.ParkingBookings.Common;

namespace ParkingBusinessResult.Api.Endpoints.ParkingBookings;

public static class ReserveBookingEndpoint
{
    public sealed record Request(Guid? ParkingLotId, string? VehicleType, string? LicensePlate, string? ExpectedArrivalAt, string? Note)
    {
        [JsonExtensionData] public Dictionary<string, JsonElement>? ExtraFields { get; init; }
    }

    public static void Map(RouteGroupBuilder routes) => routes.MapPost("/", Handle);

    private static IResult Handle(Request request, ClaimsPrincipal user, ReserveBookingHandler handler)
    {
        string[] formats = ["yyyy-MM-ddTHH:mm", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-dd HH:mm:ss"];
        if (request.ParkingLotId is null || request.VehicleType is null || request.LicensePlate is null ||
            request.ExtraFields is { Count: > 0 } || !DateTime.TryParseExact(request.ExpectedArrivalAt, formats,
                CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime arrival))
            return EndpointSupport.InvalidBody();
        var input = new ReservationInput(request.ParkingLotId.Value, request.LicensePlate, request.VehicleType, arrival, request.Note);
        BookingWriteResult result = handler.Handle(new ReserveBookingCommand(input, EndpointSupport.GetAuditActor(user)));
        return EndpointSupport.Respond(result);
    }
}
