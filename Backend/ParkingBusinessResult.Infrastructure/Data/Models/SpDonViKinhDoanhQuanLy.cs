using System;
using System.Collections.Generic;

namespace ParkingBusinessResult.Infrastructure.Data.Models;

public partial class SpDonViKinhDoanhQuanLy
{
    public string Id { get; set; } = null!;

    public string TenDonVi { get; set; } = null!;

    public string DiaChi { get; set; } = null!;

    public string NguoiLienHe { get; set; } = null!;

    public string? SoDienThoai { get; set; }

    public string? IdNguoiTao { get; set; }

    public string? IdNguoiCapNhat { get; set; }

    public string? NguoiCapNhat { get; set; }

    public string? NgayTao { get; set; }

    public string? NguoiTao { get; set; }

    public string? NgayCapNhat { get; set; }

    public string? GhiChu { get; set; }

    public virtual ICollection<DlKthtBaiDoXe> DlKthtBaiDoXe { get; set; } = new List<DlKthtBaiDoXe>();
}
