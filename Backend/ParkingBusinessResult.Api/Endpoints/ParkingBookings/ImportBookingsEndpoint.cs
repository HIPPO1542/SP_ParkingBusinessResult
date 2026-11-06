using System.Security.Claims;
using ParkingBusinessResult.Api.Contracts;
using ParkingBusinessResult.Application.Features.ParkingBookings.Commands.Import;
using ParkingBusinessResult.Application.Features.ParkingBookings.Common;

namespace ParkingBusinessResult.Api.Endpoints.ParkingBookings;

public static class ImportBookingsEndpoint
{
    private const long MaximumFileBytes = 10 * 1024 * 1024;

    public static void Map(RouteGroupBuilder routes) => routes.MapPost("/import", Handle).DisableAntiforgery();

    private static async Task<IResult> Handle(HttpRequest request, ClaimsPrincipal user, ImportBookingsHandler handler, CancellationToken cancellationToken)
    {
        if (!request.HasFormContentType || request.ContentLength > MaximumFileBytes + 65536)
            return Results.Ok(ApiResponse<object>.BusinessError("Yêu cầu phải là biểu mẫu chứa file Excel tối đa 10 MB."));
        IFormCollection form = await request.ReadFormAsync(cancellationToken);
        IFormFile? file = form.Files.GetFile("file");
        string extension = file is null ? "" : Path.GetExtension(file.FileName).ToLowerInvariant();
        if (file is null || form.Files.Count != 1 || file.Length is <= 0 or > MaximumFileBytes ||
            extension is not (".xlsx" or ".xls" or ".csv"))
            return Results.Ok(ApiResponse<object>.BusinessError("Cần một file .xlsx, .xls hoặc .csv có dung lượng tối đa 10 MB."));
        try
        {
            using Stream stream = file.OpenReadStream();
            var actor = EndpointSupport.GetAuditActor(user);
            BookingImportResult result = handler.Handle(new ImportBookingsCommand(stream, extension, EndpointSupport.GetAccess(user),
                actor.Id, actor.Name), cancellationToken);
            string message = $"Import dữ liệu hoàn tất với {result.SuccessRows} bản ghi thành công và {result.ErrorRows} bản ghi lỗi";
            return Results.Ok(ApiResponse<BookingImportResult>.Success(result, message));
        }
        catch (InvalidDataException exception)
        {
            return Results.Ok(ApiResponse<object>.BusinessError(exception.Message));
        }
    }
}
