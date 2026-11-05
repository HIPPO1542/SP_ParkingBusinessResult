-- Adds a development-local soft-delete flag. The previous table must match
-- 002; review inbound references, indexes and triggers before applying.
PRAGMA foreign_keys = ON;
BEGIN IMMEDIATE;

CREATE TABLE "SP_ketQuaKinhDoanhBaiDoXe_soft" (
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
    "daXoa" INTEGER NOT NULL DEFAULT 0 CHECK ("daXoa" IN (0, 1)),
    FOREIGN KEY ("baiDoXeId") REFERENCES "DL_KTHT_baiDoXe" ("Id")
);

INSERT INTO "SP_ketQuaKinhDoanhBaiDoXe_soft"
    ("id", "baiDoXeId", "kyBaoCao", "doanhThu", "soLuotXe", "idNguoiTao", "idNguoiCapNhat",
     "nguoiCapNhat", "ngayTao", "nguoiTao", "ngayCapNhat", "ghiChu")
    SELECT "id", "baiDoXeId", "kyBaoCao", "doanhThu", "soLuotXe", "idNguoiTao", "idNguoiCapNhat",
           "nguoiCapNhat", "ngayTao", "nguoiTao", "ngayCapNhat", "ghiChu"
    FROM "SP_ketQuaKinhDoanhBaiDoXe";

DROP TABLE "SP_ketQuaKinhDoanhBaiDoXe";
ALTER TABLE "SP_ketQuaKinhDoanhBaiDoXe_soft" RENAME TO "SP_ketQuaKinhDoanhBaiDoXe";
CREATE UNIQUE INDEX "ux_active_business_result_lot_period"
    ON "SP_ketQuaKinhDoanhBaiDoXe" ("baiDoXeId", "kyBaoCao") WHERE "daXoa" = 0;
COMMIT;
