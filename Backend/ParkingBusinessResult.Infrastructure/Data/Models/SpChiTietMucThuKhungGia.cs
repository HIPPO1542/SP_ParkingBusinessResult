using System;
using System.Collections.Generic;

namespace ParkingBusinessResult.Infrastructure.Data.Models;

public partial class SpChiTietMucThuKhungGia
{
    public string Id { get; set; } = null!;

    public string IdKhungGia { get; set; } = null!;

    public string? LoaiPhuongTien { get; set; }

    public string LoaiNgay { get; set; } = null!;

    public double SoGioDau { get; set; }

    public double GiaGioDau { get; set; }

    public double GiaGioTiepTheo { get; set; }

    public string? IdNguoiTao { get; set; }

    public string? IdNguoiCapNhat { get; set; }

    public string? NguoiCapNhat { get; set; }

    public string? NgayTao { get; set; }

    public string? NguoiTao { get; set; }

    public string? NgayCapNhat { get; set; }

    public string? GhiChu { get; set; }

    public virtual ICollection<DevParkingSessions> DevParkingSessions { get; set; } = new List<DevParkingSessions>();

    public virtual SpKhungGiaThuPhi IdKhungGiaNavigation { get; set; } = null!;
}
