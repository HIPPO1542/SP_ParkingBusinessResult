using System;
using System.Collections.Generic;

namespace ParkingBusinessResult.Infrastructure.Data.Models;

public partial class SpApDungKhungGia
{
    public string Id { get; set; } = null!;

    public string BaiDoXeId { get; set; } = null!;

    public string KhungGiaId { get; set; } = null!;

    public string NgayApDung { get; set; } = null!;

    public string? TrangThaiApDung { get; set; }

    public string? IdNguoiTao { get; set; }

    public string? IdNguoiCapNhat { get; set; }

    public string? NguoiCapNhat { get; set; }

    public string? NgayTao { get; set; }

    public string? NguoiTao { get; set; }

    public string? NgayCapNhat { get; set; }

    public string? GhiChu { get; set; }

    public virtual DlKthtBaiDoXe BaiDoXe { get; set; } = null!;

    public virtual SpKhungGiaThuPhi KhungGia { get; set; } = null!;
}
