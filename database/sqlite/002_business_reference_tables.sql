-- Apply after 001 to a database without existing result rows. Populating the
-- newly created parking-lot table and migrating existing results requires a
-- separate, reviewed procedure; otherwise the foreign key rejects the copy.
PRAGMA foreign_keys = ON;
BEGIN IMMEDIATE;

CREATE TABLE "SP_donViKinhDoanhQuanLy" (
    "id" TEXT NOT NULL PRIMARY KEY,
    "tenDonVi" TEXT NOT NULL,
    "diaChi" TEXT NOT NULL,
    "nguoiLienHe" TEXT NOT NULL,
    "soDienThoai" TEXT,
    "idNguoiTao" TEXT,
    "idNguoiCapNhat" TEXT,
    "nguoiCapNhat" TEXT,
    "ngayTao" TEXT,
    "nguoiTao" TEXT,
    "ngayCapNhat" TEXT,
    "ghiChu" TEXT
);

-- SQLite has no native GeoMultiPolygon or ObjType. The values are retained as
-- text; spatial operations and the object encoding are not defined here.
CREATE TABLE "DL_KTHT_baiDoXe" (
    "Id" TEXT NOT NULL PRIMARY KEY,
    "quyHoachId" TEXT NOT NULL,
    "donViQuanLyId" TEXT NOT NULL,
    "ten" TEXT,
    "ma" TEXT,
    "geo" TEXT NOT NULL,
    "diaChi" TEXT,
    "loaiHinh" TEXT,
    "trangThai" TEXT,
    "tongSucChua" INTEGER,
    "sucChuaHienHuuXeMoto" INTEGER,
    "sucChuaHienHuuXeOto" INTEGER,
    "sucChuaHienHuuXeLon" INTEGER,
    "ghiChu" TEXT,
    "dienTichThucTe" REAL NOT NULL,
    "idNguoiTao" TEXT,
    "idNguoiCapNhat" TEXT,
    "nguoiCapNhat" TEXT,
    "ngayTao" TEXT,
    "nguoiTao" TEXT,
    "ngayCapNhat" TEXT,
    FOREIGN KEY ("donViQuanLyId") REFERENCES "SP_donViKinhDoanhQuanLy" ("id")
);

CREATE TABLE "SP_ketQuaKinhDoanhBaiDoXe_new" (
    "id" TEXT NOT NULL PRIMARY KEY,
    "baiDoXeId" TEXT NOT NULL,
    "kyBaoCao" TEXT COLLATE BINARY NOT NULL,
    "doanhThu" REAL NOT NULL CHECK ("doanhThu" >= 0),
    "soLuotXe" INTEGER NOT NULL CHECK ("soLuotXe" >= 0),
    "idNguoiTao" TEXT,
    "idNguoiCapNhat" TEXT,
    "nguoiCapNhat" TEXT,
    "ngayTao" TEXT,
    "nguoiTao" TEXT,
    "ngayCapNhat" TEXT,
    "ghiChu" TEXT,
    UNIQUE ("baiDoXeId", "kyBaoCao"),
    FOREIGN KEY ("baiDoXeId") REFERENCES "DL_KTHT_baiDoXe" ("Id")
);

INSERT INTO "SP_ketQuaKinhDoanhBaiDoXe_new"
    SELECT "id", "baiDoXeId", "kyBaoCao", "doanhThu", "soLuotXe", "idNguoiTao", "idNguoiCapNhat",
           "nguoiCapNhat", "ngayTao", "nguoiTao", "ngayCapNhat", "ghiChu"
    FROM "SP_ketQuaKinhDoanhBaiDoXe";
DROP TABLE "SP_ketQuaKinhDoanhBaiDoXe";
ALTER TABLE "SP_ketQuaKinhDoanhBaiDoXe_new" RENAME TO "SP_ketQuaKinhDoanhBaiDoXe";

-- quyHoachId is retained without a foreign key: the sheet does not identify
-- which of its two planning entities is the target.
COMMIT;
