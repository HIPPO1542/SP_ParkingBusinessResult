using System;
using System.Collections.Generic;

namespace ParkingBusinessResult.Infrastructure.Data.Models;

public partial class SpKhungGiaThuPhi
{
    public string Id { get; set; } = null!;

    public string BaiDoXeId { get; set; } = null!;

    public string TenKhungGia { get; set; } = null!;

    public string? MoTa { get; set; }

    public string TrangThai { get; set; } = null!;

    public string NgayHieuLuc { get; set; } = null!;

    public string NgayHetHieuLuc { get; set; } = null!;

    public string? IdNguoiTao { get; set; }

    public string? IdNguoiCapNhat { get; set; }

    public string? NguoiCapNhat { get; set; }

    public string? NgayTao { get; set; }

    public string? NguoiTao { get; set; }

    public string? NgayCapNhat { get; set; }

    public string? GhiChu { get; set; }

    public virtual DlKthtBaiDoXe BaiDoXe { get; set; } = null!;

    public virtual ICollection<SpApDungKhungGia> SpApDungKhungGia { get; set; } = new List<SpApDungKhungGia>();

    public virtual ICollection<SpChiTietMucThuKhungGia> SpChiTietMucThuKhungGia { get; set; } = new List<SpChiTietMucThuKhungGia>();
}
