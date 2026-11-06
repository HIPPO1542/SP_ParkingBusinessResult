using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParkingBusinessResult.Infrastructure.Persistence.Models;

namespace ParkingBusinessResult.Infrastructure.Persistence.Configurations;

public sealed class SpApDungKhungGiaConfiguration : IEntityTypeConfiguration<SpApDungKhungGia>
{
    public void Configure(EntityTypeBuilder<SpApDungKhungGia> entity)
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
    }
}
