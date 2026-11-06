using System;
using System.Collections.Generic;

namespace ParkingBusinessResult.Infrastructure.Persistence.Models;

public partial class DevAuthUsers
{
    public string Id { get; set; } = null!;

    public string Username { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string Role { get; set; } = null!;

    public string? ParkingLotId { get; set; }
}
