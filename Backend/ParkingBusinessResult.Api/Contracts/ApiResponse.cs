namespace ParkingBusinessResult.Api.Contracts;

public sealed record ApiResponse<T>(
    int Code,
    string Message,
    T? Data)
{
    public static ApiResponse<T> Success(T data, string message = "Thành công") =>
        new(0, message, data);

    public static ApiResponse<T> BusinessError(string message) =>
        new(7, message, default);
}
