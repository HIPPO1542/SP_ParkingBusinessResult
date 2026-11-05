using System.Text.Json;
using System.Text.Json.Serialization;

namespace ParkingBusinessResult.Application.DTOs;

public sealed record CreateBusinessResultRequest(
    Guid? ParkingLotId, string? ReportingPeriod, decimal? Revenue, int? CompletedVehicleCount, string? Note)
{
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalFields { get; init; }
}

public sealed record UpdateBusinessResultRequest(decimal? Revenue, int? CompletedVehicleCount, string? Note)
{
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalFields { get; init; }
}
