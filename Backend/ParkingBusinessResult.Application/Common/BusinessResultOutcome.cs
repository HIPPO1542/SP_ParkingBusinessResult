namespace ParkingBusinessResult.Application.Common;

public sealed record BusinessResultOutcome<T>(T? Data, string? Error)
{
    public static BusinessResultOutcome<T> Success(T data) => new(data, null);
    public static BusinessResultOutcome<T> Failure(string error) => new(default, error);
}
