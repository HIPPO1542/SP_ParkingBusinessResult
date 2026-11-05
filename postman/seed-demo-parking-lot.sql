-- Tùy chọn, chỉ dùng cho database SQLite phát triển và chạy đúng một lần.
-- Không tự chạy script này từ API hay Postman. Đơn vị/bãi xe sau đây là dữ liệu mẫu.
PRAGMA foreign_keys = ON;
BEGIN;

INSERT INTO "SP_donViKinhDoanhQuanLy" ("id", "tenDonVi", "diaChi", "nguoiLienHe")
VALUES ('00000000-0000-4000-8000-000000000001', 'Đơn vị thử nghiệm', 'Địa chỉ thử nghiệm', 'Người thử nghiệm');

INSERT INTO "DL_KTHT_baiDoXe" ("Id", "quyHoachId", "donViQuanLyId", "ten", "ma", "geo", "diaChi", "dienTichThucTe")
VALUES ('00000000-0000-4000-8000-000000000002', '00000000-0000-4000-8000-000000000003',
        '00000000-0000-4000-8000-000000000001', 'Bãi xe thử nghiệm', 'TEST-001',
        '{"type":"MultiPolygon","coordinates":[[[[106,10],[106.001,10],[106.001,10.001],[106,10]]]]}',
        'Địa chỉ thử nghiệm', 100);

COMMIT;
