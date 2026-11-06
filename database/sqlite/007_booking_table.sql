-- SP_ParkingBooking in the approved workbook; actual visits may not yet have an exit time.
CREATE TABLE "SP_datTruocChoDoXe" (
    "id" TEXT NOT NULL PRIMARY KEY,
    "baiDoXeId" TEXT NOT NULL REFERENCES "DL_KTHT_baiDoXe"("Id"),
    "loaiXe" TEXT NOT NULL,
    "thoiGianBatDau" TEXT NOT NULL,
    "thoiGianKetThuc" TEXT,
    "trangThaiDatCho" TEXT NOT NULL DEFAULT 'CONFIRMED',
    "bienSoXe" TEXT NOT NULL COLLATE NOCASE,
    "soLuongChoDat" INTEGER NOT NULL,
    "idNguoiTao" TEXT,
    "idNguoiCapNhat" TEXT,
    "nguoiCapNhat" TEXT,
    "ngayTao" TEXT,
    "nguoiTao" TEXT,
    "ngayCapNhat" TEXT,
    "ghiChu" TEXT,
    "tongTien" REAL,
    UNIQUE ("baiDoXeId", "bienSoXe", "thoiGianBatDau"),
    CHECK (length(trim("bienSoXe")) > 0),
    CHECK (length(trim("loaiXe")) > 0),
    CHECK ("soLuongChoDat" > 0),
    CHECK ("thoiGianKetThuc" IS NULL OR "thoiGianKetThuc" > "thoiGianBatDau"),
    CHECK ("tongTien" IS NULL OR "tongTien" >= 0)
);
CREATE INDEX "ix_booking_exit" ON "SP_datTruocChoDoXe" ("baiDoXeId", "thoiGianKetThuc");
