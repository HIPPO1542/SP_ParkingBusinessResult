using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParkingBusinessResult.Infrastructure.Persistence.Models;

namespace ParkingBusinessResult.Infrastructure.Persistence.Configurations;

public sealed class SpKhungGiaThuPhiConfiguration : IEntityTypeConfiguration<SpKhungGiaThuPhi>
{
    public void Configure(EntityTypeBuilder<SpKhungGiaThuPhi> entity)
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
    }
}
