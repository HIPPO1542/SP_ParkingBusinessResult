-- Names follow the SP_ParkingBusinessResult sheet. UUIDs and audit IDs are text.
CREATE TABLE "SP_ketQuaKinhDoanhBaiDoXe" (
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
    UNIQUE ("baiDoXeId", "kyBaoCao")
);

-- The parking-lot entity is owned separately; its table and foreign key are
-- pending until that entity's complete SQLite mapping is established.
