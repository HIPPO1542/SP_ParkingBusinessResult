using System;
using System.Collections.Generic;

namespace ParkingBusinessResult.Infrastructure.Persistence.Models;

public partial class SpKetQuaKinhDoanhBaiDoXe
{
    public string Id { get; set; } = null!;

    public string BaiDoXeId { get; set; } = null!;

    public string KyBaoCao { get; set; } = null!;

    public double DoanhThu { get; set; }

    public int SoLuotXe { get; set; }

    public string? IdNguoiTao { get; set; }

    public string? IdNguoiCapNhat { get; set; }

    public string? NguoiCapNhat { get; set; }

    public string? NgayTao { get; set; }

    public string? NguoiTao { get; set; }

    public string? NgayCapNhat { get; set; }

    public string? GhiChu { get; set; }

    public int DaXoa { get; set; }

    public virtual DlKthtBaiDoXe BaiDoXe { get; set; } = null!;
}
