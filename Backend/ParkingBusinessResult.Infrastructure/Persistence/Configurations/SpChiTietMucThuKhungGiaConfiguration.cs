using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParkingBusinessResult.Infrastructure.Persistence.Models;

namespace ParkingBusinessResult.Infrastructure.Persistence.Configurations;

public sealed class SpChiTietMucThuKhungGiaConfiguration : IEntityTypeConfiguration<SpChiTietMucThuKhungGia>
{
    public void Configure(EntityTypeBuilder<SpChiTietMucThuKhungGia> entity)
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
    }
}
