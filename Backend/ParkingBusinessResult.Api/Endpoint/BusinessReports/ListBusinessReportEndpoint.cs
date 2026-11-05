using System.Security.Claims;
using ParkingBusinessResult.Api.Contracts;
using ParkingBusinessResult.Application.Features.BusinessReports.Queries.List;
using ParkingBusinessResult.Domain;

namespace ParkingBusinessResult.Api.Endpoint.BusinessReports;

public static class ListBusinessReportEndpoint
{
    public static void Map(RouteGroupBuilder routes) => routes.MapGet("/", Handle);

    private static IResult Handle(string? reportingPeriod, Guid? parkingLotId, string? keyword, string? pageNumber, string? pageSize,
        HttpRequest request, ClaimsPrincipal user, ListBusinessReportHandler handler)
    {
        if (string.IsNullOrWhiteSpace(reportingPeriod))
            return Results.Ok(ApiResponse<object>.BusinessError("Cần cung cấp kỳ báo cáo."));
        string[] allowedParameters = ["reportingPeriod", "parkingLotId", "keyword", "pageNumber", "pageSize"];
        if (request.Query.Keys.Any(key => !allowedParameters.Contains(key, StringComparer.OrdinalIgnoreCase)))
            return Results.Ok(ApiResponse<object>.BusinessError("Bộ lọc báo cáo chưa được hỗ trợ."));
        if (!TryReadPositiveNumber(pageNumber, 1, out int page) ||
            !TryReadPositiveNumber(pageSize, 10, out int size) || size > 100)
            return Results.Ok(ApiResponse<object>.BusinessError("Trang phải từ 1; kích thước trang phải từ 1 đến 100."));

        IReadOnlyList<ParkingBusinessResultRecord> records = handler.Handle(
            new ListBusinessReportQuery(reportingPeriod.Trim(), parkingLotId, EndpointSupport.GetAccess(user), keyword));
        long offset = ((long)page - 1) * size;
        ParkingBusinessResultRecord[] pageRecords = offset >= records.Count
            ? []
            : records.Skip((int)offset).Take(size).ToArray();
        return Results.Ok(ApiResponse<IReadOnlyList<ParkingBusinessResultRecord>>.Success(pageRecords));
    }

    private static bool TryReadPositiveNumber(string? value, int defaultValue, out int number)
    {
        if (value is null)
        {
            number = defaultValue;
            return true;
        }
        return int.TryParse(value, System.Globalization.NumberStyles.None,
            System.Globalization.CultureInfo.InvariantCulture, out number) && number > 0;
    }
}
