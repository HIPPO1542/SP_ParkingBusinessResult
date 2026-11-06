using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using ParkingBusinessResult.Infrastructure.Data.Models;

namespace ParkingBusinessResult.Infrastructure.Data;

public partial class ParkingDbContext : DbContext
{
    public ParkingDbContext(DbContextOptions<ParkingDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<DevAuthUsers> DevAuthUsers { get; set; }

    public virtual DbSet<DevParkingSessions> DevParkingSessions { get; set; }

    public virtual DbSet<DlKthtBaiDoXe> DlKthtBaiDoXe { get; set; }

    public virtual DbSet<SpApDungKhungGia> SpApDungKhungGia { get; set; }

    public virtual DbSet<SpChiTietMucThuKhungGia> SpChiTietMucThuKhungGia { get; set; }

    public virtual DbSet<SpDatTruocChoDoXe> SpDatTruocChoDoXe { get; set; }

    public virtual DbSet<SpDonViKinhDoanhQuanLy> SpDonViKinhDoanhQuanLy { get; set; }

    public virtual DbSet<SpKetQuaKinhDoanhBaiDoXe> SpKetQuaKinhDoanhBaiDoXe { get; set; }

    public virtual DbSet<SpKhungGiaThuPhi> SpKhungGiaThuPhi { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DevAuthUsers>(entity =>
        {
            entity.ToTable("dev_auth_users");

            entity.HasIndex(e => e.Username, "IX_dev_auth_users_username").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ParkingLotId).HasColumnName("parkingLotId");
            entity.Property(e => e.PasswordHash).HasColumnName("passwordHash");
            entity.Property(e => e.Role).HasColumnName("role");
            entity.Property(e => e.Username)
                .UseCollation("NOCASE")
                .HasColumnName("username");
        });

        modelBuilder.Entity<DevParkingSessions>(entity =>
        {
            entity.ToTable("dev_parking_sessions");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ChargedAmount).HasColumnName("chargedAmount");
            entity.Property(e => e.EnteredAt).HasColumnName("enteredAt");
            entity.Property(e => e.ExitedAt).HasColumnName("exitedAt");
            entity.Property(e => e.ParkingLotId).HasColumnName("parkingLotId");
            entity.Property(e => e.PaymentMethod).HasColumnName("paymentMethod");
            entity.Property(e => e.PriceRuleId).HasColumnName("priceRuleId");
            entity.Property(e => e.TestPlate).HasColumnName("testPlate");
            entity.Property(e => e.VehicleType).HasColumnName("vehicleType");

            entity.HasOne(d => d.ParkingLot).WithMany(p => p.DevParkingSessions)
                .HasForeignKey(d => d.ParkingLotId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.PriceRule).WithMany(p => p.DevParkingSessions).HasForeignKey(d => d.PriceRuleId);
        });

        modelBuilder.Entity<DlKthtBaiDoXe>(entity =>
        {
            entity.ToTable("DL_KTHT_baiDoXe");

            entity.Property(e => e.DiaChi).HasColumnName("diaChi");
            entity.Property(e => e.DienTichThucTe).HasColumnName("dienTichThucTe");
            entity.Property(e => e.DonViQuanLyId).HasColumnName("donViQuanLyId");
            entity.Property(e => e.Geo).HasColumnName("geo");
            entity.Property(e => e.GhiChu).HasColumnName("ghiChu");
            entity.Property(e => e.IdNguoiCapNhat).HasColumnName("idNguoiCapNhat");
            entity.Property(e => e.IdNguoiTao).HasColumnName("idNguoiTao");
            entity.Property(e => e.LoaiHinh).HasColumnName("loaiHinh");
            entity.Property(e => e.Ma).HasColumnName("ma");
            entity.Property(e => e.NgayCapNhat).HasColumnName("ngayCapNhat");
            entity.Property(e => e.NgayTao).HasColumnName("ngayTao");
            entity.Property(e => e.NguoiCapNhat).HasColumnName("nguoiCapNhat");
            entity.Property(e => e.NguoiTao).HasColumnName("nguoiTao");
            entity.Property(e => e.QuyHoachId).HasColumnName("quyHoachId");
            entity.Property(e => e.SucChuaHienHuuXeLon).HasColumnName("sucChuaHienHuuXeLon");
            entity.Property(e => e.SucChuaHienHuuXeMoto).HasColumnName("sucChuaHienHuuXeMoto");
            entity.Property(e => e.SucChuaHienHuuXeOto).HasColumnName("sucChuaHienHuuXeOto");
            entity.Property(e => e.Ten).HasColumnName("ten");
            entity.Property(e => e.TongSucChua).HasColumnName("tongSucChua");
            entity.Property(e => e.TrangThai).HasColumnName("trangThai");

            entity.HasOne(d => d.DonViQuanLy).WithMany(p => p.DlKthtBaiDoXe)
                .HasForeignKey(d => d.DonViQuanLyId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<SpApDungKhungGia>(entity =>
        {
            entity.ToTable("SP_apDungKhungGia");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BaiDoXeId).HasColumnName("baiDoXeId");
            entity.Property(e => e.GhiChu).HasColumnName("ghiChu");
            entity.Property(e => e.IdNguoiCapNhat).HasColumnName("idNguoiCapNhat");
            entity.Property(e => e.IdNguoiTao).HasColumnName("idNguoiTao");
            entity.Property(e => e.KhungGiaId).HasColumnName("khungGiaId");
            entity.Property(e => e.NgayApDung).HasColumnName("ngayApDung");
            entity.Property(e => e.NgayCapNhat).HasColumnName("ngayCapNhat");
            entity.Property(e => e.NgayTao).HasColumnName("ngayTao");
            entity.Property(e => e.NguoiCapNhat).HasColumnName("nguoiCapNhat");
            entity.Property(e => e.NguoiTao).HasColumnName("nguoiTao");
            entity.Property(e => e.TrangThaiApDung).HasColumnName("trangThaiApDung");

            entity.HasOne(d => d.BaiDoXe).WithMany(p => p.SpApDungKhungGia)
                .HasForeignKey(d => d.BaiDoXeId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.KhungGia).WithMany(p => p.SpApDungKhungGia)
                .HasForeignKey(d => d.KhungGiaId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<SpChiTietMucThuKhungGia>(entity =>
        {
            entity.ToTable("SP_chiTietMucThuKhungGia");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.GhiChu).HasColumnName("ghiChu");
            entity.Property(e => e.GiaGioDau).HasColumnName("giaGioDau");
            entity.Property(e => e.GiaGioTiepTheo).HasColumnName("giaGioTiepTheo");
            entity.Property(e => e.IdKhungGia).HasColumnName("idKhungGia");
            entity.Property(e => e.IdNguoiCapNhat).HasColumnName("idNguoiCapNhat");
            entity.Property(e => e.IdNguoiTao).HasColumnName("idNguoiTao");
            entity.Property(e => e.LoaiNgay).HasColumnName("loaiNgay");
            entity.Property(e => e.LoaiPhuongTien).HasColumnName("loaiPhuongTien");
            entity.Property(e => e.NgayCapNhat).HasColumnName("ngayCapNhat");
            entity.Property(e => e.NgayTao).HasColumnName("ngayTao");
            entity.Property(e => e.NguoiCapNhat).HasColumnName("nguoiCapNhat");
            entity.Property(e => e.NguoiTao).HasColumnName("nguoiTao");
            entity.Property(e => e.SoGioDau).HasColumnName("soGioDau");

            entity.HasOne(d => d.IdKhungGiaNavigation).WithMany(p => p.SpChiTietMucThuKhungGia)
                .HasForeignKey(d => d.IdKhungGia)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<SpDatTruocChoDoXe>(entity =>
        {
            entity.ToTable("SP_datTruocChoDoXe");

            entity.HasIndex(e => new { e.BaiDoXeId, e.BienSoXe, e.ThoiGianBatDau }, "IX_SP_datTruocChoDoXe_baiDoXeId_bienSoXe_thoiGianBatDau").IsUnique();

            entity.HasIndex(e => new { e.BaiDoXeId, e.ThoiGianKetThuc }, "ix_booking_exit");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BaiDoXeId).HasColumnName("baiDoXeId");
            entity.Property(e => e.BienSoXe)
                .UseCollation("NOCASE")
                .HasColumnName("bienSoXe");
            entity.Property(e => e.GhiChu).HasColumnName("ghiChu");
            entity.Property(e => e.IdNguoiCapNhat).HasColumnName("idNguoiCapNhat");
            entity.Property(e => e.IdNguoiTao).HasColumnName("idNguoiTao");
            entity.Property(e => e.LoaiXe).HasColumnName("loaiXe");
            entity.Property(e => e.NgayCapNhat).HasColumnName("ngayCapNhat");
            entity.Property(e => e.NgayTao).HasColumnName("ngayTao");
            entity.Property(e => e.NguoiCapNhat).HasColumnName("nguoiCapNhat");
            entity.Property(e => e.NguoiTao).HasColumnName("nguoiTao");
            entity.Property(e => e.SoLuongChoDat).HasColumnName("soLuongChoDat");
            entity.Property(e => e.ThoiGianBatDau).HasColumnName("thoiGianBatDau");
            entity.Property(e => e.ThoiGianKetThuc).HasColumnName("thoiGianKetThuc");
            entity.Property(e => e.TongTien).HasColumnName("tongTien");
            entity.Property(e => e.TrangThaiDatCho)
                .HasDefaultValue("CONFIRMED")
                .HasColumnName("trangThaiDatCho");

            entity.HasOne(d => d.BaiDoXe).WithMany(p => p.SpDatTruocChoDoXe)
                .HasForeignKey(d => d.BaiDoXeId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<SpDonViKinhDoanhQuanLy>(entity =>
        {
            entity.ToTable("SP_donViKinhDoanhQuanLy");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DiaChi).HasColumnName("diaChi");
            entity.Property(e => e.GhiChu).HasColumnName("ghiChu");
            entity.Property(e => e.IdNguoiCapNhat).HasColumnName("idNguoiCapNhat");
            entity.Property(e => e.IdNguoiTao).HasColumnName("idNguoiTao");
            entity.Property(e => e.NgayCapNhat).HasColumnName("ngayCapNhat");
            entity.Property(e => e.NgayTao).HasColumnName("ngayTao");
            entity.Property(e => e.NguoiCapNhat).HasColumnName("nguoiCapNhat");
            entity.Property(e => e.NguoiLienHe).HasColumnName("nguoiLienHe");
            entity.Property(e => e.NguoiTao).HasColumnName("nguoiTao");
            entity.Property(e => e.SoDienThoai).HasColumnName("soDienThoai");
            entity.Property(e => e.TenDonVi).HasColumnName("tenDonVi");
        });

        modelBuilder.Entity<SpKetQuaKinhDoanhBaiDoXe>(entity =>
        {
            entity.ToTable("SP_ketQuaKinhDoanhBaiDoXe");

            entity.HasIndex(e => new { e.BaiDoXeId, e.KyBaoCao }, "ux_active_business_result_lot_period").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BaiDoXeId).HasColumnName("baiDoXeId");
            entity.Property(e => e.DaXoa).HasColumnName("daXoa");
            entity.Property(e => e.DoanhThu).HasColumnName("doanhThu");
            entity.Property(e => e.GhiChu).HasColumnName("ghiChu");
            entity.Property(e => e.IdNguoiCapNhat).HasColumnName("idNguoiCapNhat");
            entity.Property(e => e.IdNguoiTao).HasColumnName("idNguoiTao");
            entity.Property(e => e.KyBaoCao).HasColumnName("kyBaoCao");
            entity.Property(e => e.NgayCapNhat).HasColumnName("ngayCapNhat");
            entity.Property(e => e.NgayTao).HasColumnName("ngayTao");
            entity.Property(e => e.NguoiCapNhat).HasColumnName("nguoiCapNhat");
            entity.Property(e => e.NguoiTao).HasColumnName("nguoiTao");
            entity.Property(e => e.SoLuotXe).HasColumnName("soLuotXe");

            entity.HasOne(d => d.BaiDoXe).WithMany(p => p.SpKetQuaKinhDoanhBaiDoXe)
                .HasForeignKey(d => d.BaiDoXeId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<SpKhungGiaThuPhi>(entity =>
        {
            entity.ToTable("SP_khungGiaThuPhi");

            entity.HasIndex(e => e.TenKhungGia, "IX_SP_khungGiaThuPhi_tenKhungGia").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BaiDoXeId).HasColumnName("baiDoXeId");
            entity.Property(e => e.GhiChu).HasColumnName("ghiChu");
            entity.Property(e => e.IdNguoiCapNhat).HasColumnName("idNguoiCapNhat");
            entity.Property(e => e.IdNguoiTao).HasColumnName("idNguoiTao");
            entity.Property(e => e.MoTa).HasColumnName("moTa");
            entity.Property(e => e.NgayCapNhat).HasColumnName("ngayCapNhat");
            entity.Property(e => e.NgayHetHieuLuc).HasColumnName("ngayHetHieuLuc");
            entity.Property(e => e.NgayHieuLuc).HasColumnName("ngayHieuLuc");
            entity.Property(e => e.NgayTao).HasColumnName("ngayTao");
            entity.Property(e => e.NguoiCapNhat).HasColumnName("nguoiCapNhat");
            entity.Property(e => e.NguoiTao).HasColumnName("nguoiTao");
            entity.Property(e => e.TenKhungGia).HasColumnName("tenKhungGia");
            entity.Property(e => e.TrangThai).HasColumnName("trangThai");

            entity.HasOne(d => d.BaiDoXe).WithMany(p => p.SpKhungGiaThuPhi)
                .HasForeignKey(d => d.BaiDoXeId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
