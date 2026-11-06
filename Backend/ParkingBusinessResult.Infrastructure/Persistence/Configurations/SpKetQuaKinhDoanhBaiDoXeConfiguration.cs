using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParkingBusinessResult.Infrastructure.Persistence.Models;

namespace ParkingBusinessResult.Infrastructure.Persistence.Configurations;

public sealed class SpKetQuaKinhDoanhBaiDoXeConfiguration : IEntityTypeConfiguration<SpKetQuaKinhDoanhBaiDoXe>
{
    public void Configure(EntityTypeBuilder<SpKetQuaKinhDoanhBaiDoXe> entity)
    {
        entity.ToTable("SP_ketQuaKinhDoanhBaiDoXe");

        entity.HasIndex(e => new { e.BaiDoXeId, e.KyBaoCao }, "ux_active_business_result_lot_period").IsUnique().HasFilter("\"daXoa\" = 0");

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
        entity.ToTable("SP_ketQuaKinhDoanhBaiDoXe", table => table.HasCheckConstraint("CK_SP_ketQuaKinhDoanhBaiDoXe_0", "\"doanhThu\" >= 0"));
        entity.ToTable("SP_ketQuaKinhDoanhBaiDoXe", table => table.HasCheckConstraint("CK_SP_ketQuaKinhDoanhBaiDoXe_1", "\"soLuotXe\" >= 0"));
        entity.ToTable("SP_ketQuaKinhDoanhBaiDoXe", table => table.HasCheckConstraint("CK_SP_ketQuaKinhDoanhBaiDoXe_2", "\"daXoa\" IN (0, 1)"));
    }
}
