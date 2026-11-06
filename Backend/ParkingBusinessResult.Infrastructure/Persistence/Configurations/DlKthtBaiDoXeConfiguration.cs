using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParkingBusinessResult.Infrastructure.Persistence.Models;

namespace ParkingBusinessResult.Infrastructure.Persistence.Configurations;

public sealed class DlKthtBaiDoXeConfiguration : IEntityTypeConfiguration<DlKthtBaiDoXe>
{
    public void Configure(EntityTypeBuilder<DlKthtBaiDoXe> entity)
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
    }
}
