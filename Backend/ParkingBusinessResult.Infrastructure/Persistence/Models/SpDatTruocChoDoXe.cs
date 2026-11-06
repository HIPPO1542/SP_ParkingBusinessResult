using System;
using System.Collections.Generic;

namespace ParkingBusinessResult.Infrastructure.Persistence.Models;

public partial class SpDatTruocChoDoXe
{
    public string Id { get; set; } = null!;

    public string BaiDoXeId { get; set; } = null!;

    public string LoaiXe { get; set; } = null!;

    public DateTime ThoiGianBatDau { get; set; }

    public DateTime? ThoiGianKetThuc { get; set; }

    public string TrangThaiDatCho { get; set; } = null!;

    public string BienSoXe { get; set; } = null!;

    public int SoLuongChoDat { get; set; }

    public string? IdNguoiTao { get; set; }

    public string? IdNguoiCapNhat { get; set; }

    public string? NguoiCapNhat { get; set; }

    public string? NgayTao { get; set; }

    public string? NguoiTao { get; set; }

    public string? NgayCapNhat { get; set; }

    public string? GhiChu { get; set; }

    public double? TongTien { get; set; }

    public virtual DlKthtBaiDoXe BaiDoXe { get; set; } = null!;
}
