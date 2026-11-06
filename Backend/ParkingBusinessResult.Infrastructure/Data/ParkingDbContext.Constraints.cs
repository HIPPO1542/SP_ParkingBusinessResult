using Microsoft.EntityFrameworkCore;
using ParkingBusinessResult.Infrastructure.Data.Models;
namespace ParkingBusinessResult.Infrastructure.Data;
public partial class ParkingDbContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DevParkingSessions>().ToTable("dev_parking_sessions", table => table.HasCheckConstraint("CK_dev_parking_sessions_0", "julianday(\"enteredAt\") IS NOT NULL"));
        modelBuilder.Entity<DevParkingSessions>().ToTable("dev_parking_sessions", table => table.HasCheckConstraint("CK_dev_parking_sessions_1", "\"exitedAt\" IS NULL OR (julianday(\"exitedAt\") IS NOT NULL AND julianday(\"exitedAt\") > julianday(\"enteredAt\"))"));
        modelBuilder.Entity<DevParkingSessions>().ToTable("dev_parking_sessions", table => table.HasCheckConstraint("CK_dev_parking_sessions_2", "(\"exitedAt\" IS NULL AND \"priceRuleId\" IS NULL AND \"chargedAmount\" IS NULL AND \"paymentMethod\" IS NULL)\n        OR (\"exitedAt\" IS NOT NULL AND \"priceRuleId\" IS NOT NULL AND \"chargedAmount\" IS NOT NULL\n            AND \"chargedAmount\" >= 0 AND \"paymentMethod\" IS NOT NULL AND \"paymentMethod\" IN ('CASH', 'ELECTRONIC'))"));
        modelBuilder.Entity<SpDatTruocChoDoXe>().ToTable("SP_datTruocChoDoXe", table => table.HasCheckConstraint("CK_SP_datTruocChoDoXe_0", "length(trim(\"bienSoXe\")) > 0"));
        modelBuilder.Entity<SpDatTruocChoDoXe>().ToTable("SP_datTruocChoDoXe", table => table.HasCheckConstraint("CK_SP_datTruocChoDoXe_1", "length(trim(\"loaiXe\")) > 0"));
        modelBuilder.Entity<SpDatTruocChoDoXe>().ToTable("SP_datTruocChoDoXe", table => table.HasCheckConstraint("CK_SP_datTruocChoDoXe_2", "\"soLuongChoDat\" > 0"));
        modelBuilder.Entity<SpDatTruocChoDoXe>().ToTable("SP_datTruocChoDoXe", table => table.HasCheckConstraint("CK_SP_datTruocChoDoXe_3", "\"thoiGianKetThuc\" IS NULL OR \"thoiGianKetThuc\" > \"thoiGianBatDau\""));
        modelBuilder.Entity<SpDatTruocChoDoXe>().ToTable("SP_datTruocChoDoXe", table => table.HasCheckConstraint("CK_SP_datTruocChoDoXe_4", "\"tongTien\" IS NULL OR \"tongTien\" >= 0"));
        modelBuilder.Entity<SpKetQuaKinhDoanhBaiDoXe>().ToTable("SP_ketQuaKinhDoanhBaiDoXe", table => table.HasCheckConstraint("CK_SP_ketQuaKinhDoanhBaiDoXe_0", "\"doanhThu\" >= 0"));
        modelBuilder.Entity<SpKetQuaKinhDoanhBaiDoXe>().ToTable("SP_ketQuaKinhDoanhBaiDoXe", table => table.HasCheckConstraint("CK_SP_ketQuaKinhDoanhBaiDoXe_1", "\"soLuotXe\" >= 0"));
        modelBuilder.Entity<SpKetQuaKinhDoanhBaiDoXe>().ToTable("SP_ketQuaKinhDoanhBaiDoXe", table => table.HasCheckConstraint("CK_SP_ketQuaKinhDoanhBaiDoXe_2", "\"daXoa\" IN (0, 1)"));
    }
}
