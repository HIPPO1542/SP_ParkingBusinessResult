using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParkingBusinessResult.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialParkingSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE TABLE \"DL_KTHT_baiDoXe\" (\n    \"Id\" TEXT NOT NULL PRIMARY KEY,\n    \"quyHoachId\" TEXT NOT NULL,\n    \"donViQuanLyId\" TEXT NOT NULL,\n    \"ten\" TEXT,\n    \"ma\" TEXT,\n    \"geo\" TEXT NOT NULL,\n    \"diaChi\" TEXT,\n    \"loaiHinh\" TEXT,\n    \"trangThai\" TEXT,\n    \"tongSucChua\" INTEGER,\n    \"sucChuaHienHuuXeMoto\" INTEGER,\n    \"sucChuaHienHuuXeOto\" INTEGER,\n    \"sucChuaHienHuuXeLon\" INTEGER,\n    \"ghiChu\" TEXT,\n    \"dienTichThucTe\" REAL NOT NULL,\n    \"idNguoiTao\" TEXT,\n    \"idNguoiCapNhat\" TEXT,\n    \"nguoiCapNhat\" TEXT,\n    \"ngayTao\" TEXT,\n    \"nguoiTao\" TEXT,\n    \"ngayCapNhat\" TEXT,\n    FOREIGN KEY (\"donViQuanLyId\") REFERENCES \"SP_donViKinhDoanhQuanLy\" (\"id\")\n)");
            migrationBuilder.Sql("CREATE TABLE \"SP_apDungKhungGia\" (\n    \"id\" TEXT NOT NULL PRIMARY KEY,\n    \"baiDoXeId\" TEXT NOT NULL,\n    \"khungGiaId\" TEXT NOT NULL,\n    \"ngayApDung\" TEXT NOT NULL,\n    \"trangThaiApDung\" TEXT,\n    \"idNguoiTao\" TEXT,\n    \"idNguoiCapNhat\" TEXT,\n    \"nguoiCapNhat\" TEXT,\n    \"ngayTao\" TEXT,\n    \"nguoiTao\" TEXT,\n    \"ngayCapNhat\" TEXT,\n    \"ghiChu\" TEXT,\n    FOREIGN KEY (\"baiDoXeId\") REFERENCES \"DL_KTHT_baiDoXe\" (\"Id\"),\n    FOREIGN KEY (\"khungGiaId\") REFERENCES \"SP_khungGiaThuPhi\" (\"id\")\n)");
            migrationBuilder.Sql("CREATE TABLE \"SP_chiTietMucThuKhungGia\" (\n    \"id\" TEXT NOT NULL PRIMARY KEY,\n    \"idKhungGia\" TEXT NOT NULL,\n    \"loaiPhuongTien\" TEXT,\n    \"loaiNgay\" TEXT NOT NULL,\n    \"soGioDau\" REAL NOT NULL,\n    \"giaGioDau\" REAL NOT NULL,\n    \"giaGioTiepTheo\" REAL NOT NULL,\n    \"idNguoiTao\" TEXT,\n    \"idNguoiCapNhat\" TEXT,\n    \"nguoiCapNhat\" TEXT,\n    \"ngayTao\" TEXT,\n    \"nguoiTao\" TEXT,\n    \"ngayCapNhat\" TEXT,\n    \"ghiChu\" TEXT,\n    FOREIGN KEY (\"idKhungGia\") REFERENCES \"SP_khungGiaThuPhi\" (\"id\")\n)");
            migrationBuilder.Sql("CREATE TABLE \"SP_datTruocChoDoXe\" (\n    \"id\" TEXT NOT NULL PRIMARY KEY,\n    \"baiDoXeId\" TEXT NOT NULL REFERENCES \"DL_KTHT_baiDoXe\"(\"Id\"),\n    \"loaiXe\" TEXT NOT NULL,\n    \"thoiGianBatDau\" TEXT NOT NULL,\n    \"thoiGianKetThuc\" TEXT,\n    \"trangThaiDatCho\" TEXT NOT NULL DEFAULT 'CONFIRMED',\n    \"bienSoXe\" TEXT NOT NULL COLLATE NOCASE,\n    \"soLuongChoDat\" INTEGER NOT NULL,\n    \"idNguoiTao\" TEXT,\n    \"idNguoiCapNhat\" TEXT,\n    \"nguoiCapNhat\" TEXT,\n    \"ngayTao\" TEXT,\n    \"nguoiTao\" TEXT,\n    \"ngayCapNhat\" TEXT,\n    \"ghiChu\" TEXT,\n    \"tongTien\" REAL,\n    UNIQUE (\"baiDoXeId\", \"bienSoXe\", \"thoiGianBatDau\"),\n    CHECK (length(trim(\"bienSoXe\")) > 0),\n    CHECK (length(trim(\"loaiXe\")) > 0),\n    CHECK (\"soLuongChoDat\" > 0),\n    CHECK (\"thoiGianKetThuc\" IS NULL OR \"thoiGianKetThuc\" > \"thoiGianBatDau\"),\n    CHECK (\"tongTien\" IS NULL OR \"tongTien\" >= 0)\n)");
            migrationBuilder.Sql("CREATE TABLE \"SP_donViKinhDoanhQuanLy\" (\n    \"id\" TEXT NOT NULL PRIMARY KEY,\n    \"tenDonVi\" TEXT NOT NULL,\n    \"diaChi\" TEXT NOT NULL,\n    \"nguoiLienHe\" TEXT NOT NULL,\n    \"soDienThoai\" TEXT,\n    \"idNguoiTao\" TEXT,\n    \"idNguoiCapNhat\" TEXT,\n    \"nguoiCapNhat\" TEXT,\n    \"ngayTao\" TEXT,\n    \"nguoiTao\" TEXT,\n    \"ngayCapNhat\" TEXT,\n    \"ghiChu\" TEXT\n)");
            migrationBuilder.Sql("CREATE TABLE \"SP_ketQuaKinhDoanhBaiDoXe\" (\n    \"id\" TEXT NOT NULL PRIMARY KEY,\n    \"baiDoXeId\" TEXT NOT NULL,\n    \"kyBaoCao\" TEXT COLLATE BINARY NOT NULL,\n    \"doanhThu\" REAL NOT NULL CHECK (\"doanhThu\" >= 0),\n    \"soLuotXe\" INTEGER NOT NULL CHECK (\"soLuotXe\" >= 0),\n    \"idNguoiTao\" TEXT,\n    \"idNguoiCapNhat\" TEXT,\n    \"nguoiCapNhat\" TEXT,\n    \"ngayTao\" TEXT,\n    \"nguoiTao\" TEXT,\n    \"ngayCapNhat\" TEXT,\n    \"ghiChu\" TEXT,\n    \"daXoa\" INTEGER NOT NULL DEFAULT 0 CHECK (\"daXoa\" IN (0, 1)),\n    FOREIGN KEY (\"baiDoXeId\") REFERENCES \"DL_KTHT_baiDoXe\" (\"Id\")\n)");
            migrationBuilder.Sql("CREATE TABLE \"SP_khungGiaThuPhi\" (\n    \"id\" TEXT NOT NULL PRIMARY KEY,\n    \"baiDoXeId\" TEXT NOT NULL,\n    \"tenKhungGia\" TEXT NOT NULL UNIQUE,\n    \"moTa\" TEXT,\n    \"trangThai\" TEXT NOT NULL,\n    \"ngayHieuLuc\" TEXT NOT NULL,\n    \"ngayHetHieuLuc\" TEXT NOT NULL,\n    \"idNguoiTao\" TEXT,\n    \"idNguoiCapNhat\" TEXT,\n    \"nguoiCapNhat\" TEXT,\n    \"ngayTao\" TEXT,\n    \"nguoiTao\" TEXT,\n    \"ngayCapNhat\" TEXT,\n    \"ghiChu\" TEXT,\n    FOREIGN KEY (\"baiDoXeId\") REFERENCES \"DL_KTHT_baiDoXe\" (\"Id\")\n)");
            migrationBuilder.Sql("CREATE TABLE \"dev_auth_users\" (\n    \"id\" TEXT NOT NULL PRIMARY KEY,\n    \"username\" TEXT NOT NULL COLLATE NOCASE UNIQUE,\n    \"passwordHash\" TEXT NOT NULL,\n    \"role\" TEXT NOT NULL,\n    \"parkingLotId\" TEXT\n)");
            migrationBuilder.Sql("CREATE TABLE \"dev_parking_sessions\" (\n    \"id\" TEXT NOT NULL PRIMARY KEY,\n    \"parkingLotId\" TEXT NOT NULL,\n    \"vehicleType\" TEXT NOT NULL,\n    \"testPlate\" TEXT NOT NULL,\n    \"enteredAt\" TEXT NOT NULL,\n    \"exitedAt\" TEXT,\n    \"priceRuleId\" TEXT,\n    \"chargedAmount\" REAL,\n    \"paymentMethod\" TEXT,\n    FOREIGN KEY (\"parkingLotId\") REFERENCES \"DL_KTHT_baiDoXe\" (\"Id\"),\n    FOREIGN KEY (\"priceRuleId\") REFERENCES \"SP_chiTietMucThuKhungGia\" (\"id\"),\n    CHECK (julianday(\"enteredAt\") IS NOT NULL),\n    CHECK (\"exitedAt\" IS NULL OR (julianday(\"exitedAt\") IS NOT NULL AND julianday(\"exitedAt\") > julianday(\"enteredAt\"))),\n    CHECK ((\"exitedAt\" IS NULL AND \"priceRuleId\" IS NULL AND \"chargedAmount\" IS NULL AND \"paymentMethod\" IS NULL)\n        OR (\"exitedAt\" IS NOT NULL AND \"priceRuleId\" IS NOT NULL AND \"chargedAmount\" IS NOT NULL\n            AND \"chargedAmount\" >= 0 AND \"paymentMethod\" IS NOT NULL AND \"paymentMethod\" IN ('CASH', 'ELECTRONIC')))\n)");
            migrationBuilder.Sql("CREATE INDEX \"ix_booking_exit\" ON \"SP_datTruocChoDoXe\" (\"baiDoXeId\", \"thoiGianKetThuc\")");
            migrationBuilder.Sql("CREATE UNIQUE INDEX \"ux_active_business_result_lot_period\"\n    ON \"SP_ketQuaKinhDoanhBaiDoXe\" (\"baiDoXeId\", \"kyBaoCao\") WHERE \"daXoa\" = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "dev_auth_users");

            migrationBuilder.DropTable(
                name: "dev_parking_sessions");

            migrationBuilder.DropTable(
                name: "SP_apDungKhungGia");

            migrationBuilder.DropTable(
                name: "SP_datTruocChoDoXe");

            migrationBuilder.DropTable(
                name: "SP_ketQuaKinhDoanhBaiDoXe");

            migrationBuilder.DropTable(
                name: "SP_chiTietMucThuKhungGia");

            migrationBuilder.DropTable(
                name: "SP_khungGiaThuPhi");

            migrationBuilder.DropTable(
                name: "DL_KTHT_baiDoXe");

            migrationBuilder.DropTable(
                name: "SP_donViKinhDoanhQuanLy");
        }
    }
}
