namespace ParkingBusinessResult.Application.Common;

public static class DistrictNames
{
    public static string? FromAddress(string? address)
    {
        if (address is null) return null;
        string[] matches = address.Split(',').Select(part => part.Trim())
            .Where(part => part.StartsWith("Quận ", StringComparison.OrdinalIgnoreCase) || part.StartsWith("Huyện ", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        return matches.Length == 1 ? matches[0] : null;
    }
}
