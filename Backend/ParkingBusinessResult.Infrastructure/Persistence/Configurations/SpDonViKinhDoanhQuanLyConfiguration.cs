using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParkingBusinessResult.Infrastructure.Persistence.Models;

namespace ParkingBusinessResult.Infrastructure.Persistence.Configurations;

public sealed class SpDonViKinhDoanhQuanLyConfiguration : IEntityTypeConfiguration<SpDonViKinhDoanhQuanLy>
{
    public void Configure(EntityTypeBuilder<SpDonViKinhDoanhQuanLy> entity)
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
    }
}
