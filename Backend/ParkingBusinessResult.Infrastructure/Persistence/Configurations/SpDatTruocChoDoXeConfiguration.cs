using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParkingBusinessResult.Infrastructure.Persistence.Models;

namespace ParkingBusinessResult.Infrastructure.Persistence.Configurations;

public sealed class SpDatTruocChoDoXeConfiguration : IEntityTypeConfiguration<SpDatTruocChoDoXe>
{
    public void Configure(EntityTypeBuilder<SpDatTruocChoDoXe> entity)
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
        entity.ToTable("SP_datTruocChoDoXe", table => table.HasCheckConstraint("CK_SP_datTruocChoDoXe_0", "length(trim(\"bienSoXe\")) > 0"));
        entity.ToTable("SP_datTruocChoDoXe", table => table.HasCheckConstraint("CK_SP_datTruocChoDoXe_1", "length(trim(\"loaiXe\")) > 0"));
        entity.ToTable("SP_datTruocChoDoXe", table => table.HasCheckConstraint("CK_SP_datTruocChoDoXe_2", "\"soLuongChoDat\" > 0"));
        entity.ToTable("SP_datTruocChoDoXe", table => table.HasCheckConstraint("CK_SP_datTruocChoDoXe_3", "\"thoiGianKetThuc\" IS NULL OR \"thoiGianKetThuc\" > \"thoiGianBatDau\""));
        entity.ToTable("SP_datTruocChoDoXe", table => table.HasCheckConstraint("CK_SP_datTruocChoDoXe_4", "\"tongTien\" IS NULL OR \"tongTien\" >= 0"));
    }
}
