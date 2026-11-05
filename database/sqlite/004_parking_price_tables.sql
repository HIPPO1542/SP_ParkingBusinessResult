-- SQLite development mappings of the three price sheets in the workbook.
-- ObjType and dates are text because the workbook does not define a SQLite mapping.
CREATE TABLE "SP_khungGiaThuPhi" (
    "id" TEXT NOT NULL PRIMARY KEY,
    "baiDoXeId" TEXT NOT NULL,
    "tenKhungGia" TEXT NOT NULL UNIQUE,
    "moTa" TEXT,
    "trangThai" TEXT NOT NULL,
    "ngayHieuLuc" TEXT NOT NULL,
    "ngayHetHieuLuc" TEXT NOT NULL,
    "idNguoiTao" TEXT,
    "idNguoiCapNhat" TEXT,
    "nguoiCapNhat" TEXT,
    "ngayTao" TEXT,
    "nguoiTao" TEXT,
    "ngayCapNhat" TEXT,
    "ghiChu" TEXT,
    FOREIGN KEY ("baiDoXeId") REFERENCES "DL_KTHT_baiDoXe" ("Id")
);

CREATE TABLE "SP_chiTietMucThuKhungGia" (
    "id" TEXT NOT NULL PRIMARY KEY,
    "idKhungGia" TEXT NOT NULL,
    "loaiPhuongTien" TEXT,
    "loaiNgay" TEXT NOT NULL,
    "soGioDau" REAL NOT NULL,
    "giaGioDau" REAL NOT NULL,
    "giaGioTiepTheo" REAL NOT NULL,
    "idNguoiTao" TEXT,
    "idNguoiCapNhat" TEXT,
    "nguoiCapNhat" TEXT,
    "ngayTao" TEXT,
    "nguoiTao" TEXT,
    "ngayCapNhat" TEXT,
    "ghiChu" TEXT,
    FOREIGN KEY ("idKhungGia") REFERENCES "SP_khungGiaThuPhi" ("id")
);

CREATE TABLE "SP_apDungKhungGia" (
    "id" TEXT NOT NULL PRIMARY KEY,
    "baiDoXeId" TEXT NOT NULL,
    "khungGiaId" TEXT NOT NULL,
    "ngayApDung" TEXT NOT NULL,
    "trangThaiApDung" TEXT,
    "idNguoiTao" TEXT,
    "idNguoiCapNhat" TEXT,
    "nguoiCapNhat" TEXT,
    "ngayTao" TEXT,
    "nguoiTao" TEXT,
    "ngayCapNhat" TEXT,
    "ghiChu" TEXT,
    FOREIGN KEY ("baiDoXeId") REFERENCES "DL_KTHT_baiDoXe" ("Id"),
    FOREIGN KEY ("khungGiaId") REFERENCES "SP_khungGiaThuPhi" ("id")
);
