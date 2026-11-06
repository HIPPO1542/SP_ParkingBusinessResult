using System;
using System.Collections.Generic;

namespace ParkingBusinessResult.Infrastructure.Persistence.Models;

public partial class DlKthtBaiDoXe
{
    public string Id { get; set; } = null!;

    public string QuyHoachId { get; set; } = null!;

    public string DonViQuanLyId { get; set; } = null!;

    public string? Ten { get; set; }

    public string? Ma { get; set; }

    public string Geo { get; set; } = null!;

    public string? DiaChi { get; set; }

    public string? LoaiHinh { get; set; }

    public string? TrangThai { get; set; }

    public int? TongSucChua { get; set; }

    public int? SucChuaHienHuuXeMoto { get; set; }

    public int? SucChuaHienHuuXeOto { get; set; }

    public int? SucChuaHienHuuXeLon { get; set; }

    public string? GhiChu { get; set; }

    public double DienTichThucTe { get; set; }

    public string? IdNguoiTao { get; set; }

    public string? IdNguoiCapNhat { get; set; }

    public string? NguoiCapNhat { get; set; }

    public string? NgayTao { get; set; }

    public string? NguoiTao { get; set; }

    public string? NgayCapNhat { get; set; }

    public virtual ICollection<DevParkingSessions> DevParkingSessions { get; set; } = new List<DevParkingSessions>();

    public virtual SpDonViKinhDoanhQuanLy DonViQuanLy { get; set; } = null!;

    public virtual ICollection<SpApDungKhungGia> SpApDungKhungGia { get; set; } = new List<SpApDungKhungGia>();

    public virtual ICollection<SpDatTruocChoDoXe> SpDatTruocChoDoXe { get; set; } = new List<SpDatTruocChoDoXe>();

    public virtual ICollection<SpKetQuaKinhDoanhBaiDoXe> SpKetQuaKinhDoanhBaiDoXe { get; set; } = new List<SpKetQuaKinhDoanhBaiDoXe>();

    public virtual ICollection<SpKhungGiaThuPhi> SpKhungGiaThuPhi { get; set; } = new List<SpKhungGiaThuPhi>();
}
