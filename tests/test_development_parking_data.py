import sqlite3
import subprocess
import sys
import tempfile
import unittest
from contextlib import closing
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
SQLITE = ROOT / "database/sqlite"
SCRIPT = SQLITE / "seed_development_parking_data.py"
AS_OF = "2026-11-05T12:00:00+07:00"


def prepare_database(path):
    with closing(sqlite3.connect(path)) as database:
        for name in ("001_create_business_result.sql", "002_business_reference_tables.sql", "003_soft_delete_business_result.sql"):
            database.executescript((SQLITE / name).read_text(encoding="utf-8"))
        database.execute('INSERT INTO "SP_donViKinhDoanhQuanLy" (id, tenDonVi, diaChi, nguoiLienHe) VALUES (?, ?, ?, ?)',
                         ("existing-unit", "Existing", "Address", "Contact"))
        database.execute('INSERT INTO "DL_KTHT_baiDoXe" (Id, quyHoachId, donViQuanLyId, geo, dienTichThucTe) VALUES (?, ?, ?, ?, ?)',
                         ("existing-lot", "existing-plan", "existing-unit", "{}", 10))
        database.execute('INSERT INTO "SP_ketQuaKinhDoanhBaiDoXe" (id, baiDoXeId, kyBaoCao, doanhThu, soLuotXe) VALUES (?, ?, ?, ?, ?)',
                         ("existing-result", "existing-lot", "2026-10", 777, 3))
        database.executescript((SQLITE / "007_booking_table.sql").read_text(encoding="utf-8"))
        database.execute('INSERT INTO SP_datTruocChoDoXe (id,baiDoXeId,loaiXe,thoiGianBatDau,thoiGianKetThuc,bienSoXe,soLuongChoDat,tongTien) VALUES (?,?,?,?,?,?,?,?)',
                         ("existing-booking", "existing-lot", "MOTORBIKE", "2026-11-04 08:00:00", "2026-11-04 09:00:00", "BOOKING-EXISTING", 1, 12345))
        database.commit()


def run_seed(path):
    return subprocess.run([sys.executable, str(SCRIPT), "--database", str(path), "--as-of", AS_OF],
                          capture_output=True, text=True, encoding="utf-8", check=False)


class DevelopmentParkingDataTests(unittest.TestCase):
    def test_counts_tariffs_totals_backup_and_repeat_without_creating_visits(self):
        with tempfile.TemporaryDirectory() as temporary_directory:
            database_path = Path(temporary_directory) / "parking.db"
            prepare_database(database_path)
            process = run_seed(database_path)
            self.assertEqual(process.returncode, 0, process.stderr)
            backups = list(database_path.parent.glob("parking.backup-demo-*.db"))
            self.assertEqual(len(backups), 1)
            with closing(sqlite3.connect(backups[0])) as backup:
                self.assertEqual(backup.execute('SELECT COUNT(*) FROM "DL_KTHT_baiDoXe"').fetchone(), (1,))
                self.assertEqual(backup.execute('SELECT doanhThu FROM "SP_ketQuaKinhDoanhBaiDoXe" WHERE id = ?', ("existing-result",)).fetchone(), (777,))

            with closing(sqlite3.connect(database_path)) as database:
                self.assertEqual(database.execute("PRAGMA integrity_check").fetchone(), ("ok",))
                self.assertEqual(database.execute("PRAGMA foreign_key_check").fetchall(), [])
                expected_columns = {
                    "SP_khungGiaThuPhi": {"id", "baiDoXeId", "tenKhungGia", "moTa", "trangThai", "ngayHieuLuc", "ngayHetHieuLuc", "idNguoiTao", "idNguoiCapNhat", "nguoiCapNhat", "ngayTao", "nguoiTao", "ngayCapNhat", "ghiChu"},
                    "SP_chiTietMucThuKhungGia": {"id", "idKhungGia", "loaiPhuongTien", "loaiNgay", "soGioDau", "giaGioDau", "giaGioTiepTheo", "idNguoiTao", "idNguoiCapNhat", "nguoiCapNhat", "ngayTao", "nguoiTao", "ngayCapNhat", "ghiChu"},
                    "SP_apDungKhungGia": {"id", "baiDoXeId", "khungGiaId", "ngayApDung", "trangThaiApDung", "idNguoiTao", "idNguoiCapNhat", "nguoiCapNhat", "ngayTao", "nguoiTao", "ngayCapNhat", "ghiChu"},
                }
                for table, expected in expected_columns.items():
                    actual = {row[1] for row in database.execute("SELECT * FROM pragma_table_info(?)", (table,))}
                    self.assertEqual(actual, expected)
                self.assertEqual(database.execute('SELECT COUNT(*) FROM "DL_KTHT_baiDoXe" WHERE ma LIKE ?', ("DEMO-%",)).fetchone(), (5,))
                self.assertEqual(database.execute('SELECT COUNT(*) FROM "SP_khungGiaThuPhi"').fetchone(), (5,))
                self.assertEqual(database.execute('SELECT COUNT(*) FROM "SP_chiTietMucThuKhungGia"').fetchone(), (20,))
                self.assertEqual(database.execute('SELECT COUNT(*) FROM "SP_apDungKhungGia"').fetchone(), (5,))
                names = {row[0] for row in database.execute("SELECT name FROM sqlite_master WHERE type='table'")}
                self.assertNotIn("dev_parking_sessions", names)
                summaries = database.execute(
                    'SELECT r.doanhThu,r.soLuotXe FROM SP_ketQuaKinhDoanhBaiDoXe r JOIN DL_KTHT_baiDoXe p ON p.Id=r.baiDoXeId WHERE p.ma LIKE ? AND r.kyBaoCao=?',
                    ("DEMO-%", "2026-11"),
                ).fetchall()
                self.assertEqual(summaries, [(555000, 30)] * 5)
                rates = set(database.execute('SELECT loaiPhuongTien,loaiNgay,soGioDau,giaGioDau,giaGioTiepTheo FROM SP_chiTietMucThuKhungGia'))
                self.assertEqual(rates, {("MOTORBIKE", "WEEKDAY", 1, 5000, 2000), ("MOTORBIKE", "WEEKEND", 1, 7000, 3000),
                                         ("CAR_9_SEATS", "WEEKDAY", 1, 20000, 10000), ("CAR_9_SEATS", "WEEKEND", 1, 25000, 12000)})
                booking = database.execute('SELECT id,bienSoXe,tongTien FROM SP_datTruocChoDoXe').fetchall()
                self.assertEqual(booking, [("existing-booking", "BOOKING-EXISTING", 12345)])

                self.assertEqual(database.execute(
                    'SELECT doanhThu, soLuotXe FROM "SP_ketQuaKinhDoanhBaiDoXe" WHERE id = ?', ("existing-result",)
                ).fetchone(), (777, 3))

            repeated = run_seed(database_path)
            self.assertNotEqual(repeated.returncode, 0)
            with closing(sqlite3.connect(database_path)) as database:
                self.assertEqual(database.execute("SELECT COUNT(*) FROM SP_ketQuaKinhDoanhBaiDoXe").fetchone(), (6,))
                self.assertEqual(database.execute("SELECT COUNT(*) FROM SP_datTruocChoDoXe").fetchone(), (1,))

    def test_refusal_preserves_legacy_visits_and_bookings(self):
        with tempfile.TemporaryDirectory() as temporary_directory:
            database_path = Path(temporary_directory) / "parking.db"
            prepare_database(database_path)
            with closing(sqlite3.connect(database_path)) as database:
                for name in ("004_parking_price_tables.sql", "005_dev_parking_sessions.sql"):
                    database.executescript((SQLITE / name).read_text(encoding="utf-8"))
                database.execute('INSERT INTO dev_parking_sessions (id,parkingLotId,vehicleType,testPlate,enteredAt) VALUES (?,?,?,?,?)',
                                 ("legacy-session", "existing-lot", "MOTORBIKE", "LEGACY-EXISTING", "2026-11-04T08:00:00+07:00"))
                database.commit()
                legacy_before = database.execute('SELECT * FROM dev_parking_sessions').fetchall()
                booking_before = database.execute('SELECT * FROM SP_datTruocChoDoXe').fetchall()
            process = run_seed(database_path)
            self.assertNotEqual(process.returncode, 0)
            with closing(sqlite3.connect(database_path)) as database:
                self.assertEqual(database.execute('SELECT * FROM dev_parking_sessions').fetchall(), legacy_before)
                self.assertEqual(database.execute('SELECT * FROM SP_datTruocChoDoXe').fetchall(), booking_before)

    def test_rolls_back_schema_and_data_when_insert_fails(self):
        with tempfile.TemporaryDirectory() as temporary_directory:
            database_path = Path(temporary_directory) / "parking.db"
            prepare_database(database_path)
            with closing(sqlite3.connect(database_path)) as database:
                database.execute("CREATE TRIGGER reject_demo BEFORE INSERT ON SP_ketQuaKinhDoanhBaiDoXe "
                                 "WHEN NEW.nguoiTao = 'du_lieu_gia_lap' BEGIN SELECT RAISE(ABORT, 'test failure'); END")
                database.commit()
            process = run_seed(database_path)
            self.assertNotEqual(process.returncode, 0)
            with closing(sqlite3.connect(database_path)) as database:
                names = {row[0] for row in database.execute("SELECT name FROM sqlite_master WHERE type = 'table'")}
                self.assertNotIn("dev_parking_sessions", names)
                self.assertNotIn("SP_khungGiaThuPhi", names)
                self.assertEqual(database.execute('SELECT COUNT(*) FROM "DL_KTHT_baiDoXe"').fetchone(), (1,))
                self.assertEqual(database.execute('SELECT COUNT(*) FROM "SP_ketQuaKinhDoanhBaiDoXe"').fetchone(), (1,))


if __name__ == "__main__":
    unittest.main()
