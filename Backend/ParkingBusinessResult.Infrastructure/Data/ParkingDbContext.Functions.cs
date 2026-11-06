using Microsoft.EntityFrameworkCore;

namespace ParkingBusinessResult.Infrastructure.Data;

public partial class ParkingDbContext
{
    [DbFunction("contains_ignore_case", IsBuiltIn = true)]
    public static bool ContainsIgnoreCase(string? value, string keyword) => throw new NotSupportedException("Use this function only in a database query.");
}
