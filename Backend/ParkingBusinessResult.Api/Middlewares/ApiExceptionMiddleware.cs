using System.Diagnostics;
using System.Text.Json;
using ParkingBusinessResult.Api.Contracts;

namespace ParkingBusinessResult.Api.Middlewares;

public sealed class ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        long started = Stopwatch.GetTimestamp();
        string requestId = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
        context.Response.Headers["X-Request-Id"] = requestId;
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers.CacheControl = "no-store";
        using IDisposable? scope = logger.BeginScope(new Dictionary<string, object> { ["RequestId"] = requestId });
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            logger.LogDebug("Request was cancelled by the caller.");
        }
        catch (Exception error) when (!context.Response.HasStarted)
        {
            bool invalidRequest = error is BadHttpRequestException or JsonException or InvalidDataException;
            if (invalidRequest) logger.LogWarning("Invalid request: {ErrorType}", error.GetType().Name);
            else logger.LogError(error, "Request processing failed.");
            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status200OK;
            context.Response.Headers["X-Request-Id"] = requestId;
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers.CacheControl = "no-store";
            string message = invalidRequest ? "Dữ liệu yêu cầu không hợp lệ." : "Không thể xử lý yêu cầu. Vui lòng thử lại hoặc cung cấp mã yêu cầu cho người quản trị.";
            await context.Response.WriteAsJsonAsync(ApiResponse<object>.BusinessError(message), context.RequestAborted);
        }
        finally
        {
            logger.LogInformation("{Method} {Path} completed with HTTP {StatusCode} in {ElapsedMs} ms",
                context.Request.Method, context.Request.Path, context.Response.StatusCode, Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        }
    }
}
