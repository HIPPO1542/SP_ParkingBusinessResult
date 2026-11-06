using System;
using System.Collections.Generic;

namespace ParkingBusinessResult.Infrastructure.Data.Models;

public partial class DevParkingSessions
{
    public string Id { get; set; } = null!;

    public string ParkingLotId { get; set; } = null!;

    public string VehicleType { get; set; } = null!;

    public string TestPlate { get; set; } = null!;

    public string EnteredAt { get; set; } = null!;

    public string? ExitedAt { get; set; }

    public string? PriceRuleId { get; set; }

    public double? ChargedAmount { get; set; }

    public string? PaymentMethod { get; set; }

    public virtual DlKthtBaiDoXe ParkingLot { get; set; } = null!;

    public virtual SpChiTietMucThuKhungGia? PriceRule { get; set; }
}
