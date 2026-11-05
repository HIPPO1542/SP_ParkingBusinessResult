import sqlite3
import subprocess
import sys
import tempfile
import unittest
import uuid
from contextlib import closing
from datetime import datetime
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
        database.commit()


def run_seed(path):
    return subprocess.run([sys.executable, str(SCRIPT), "--database", str(path), "--as-of", AS_OF],
                          capture_output=True, text=True, encoding="utf-8", check=False)


class DevelopmentParkingDataTests(unittest.TestCase):
    def test_counts_tariffs_totals_backup_and_repeat(self):
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
                self.assertEqual(database.execute(
                    'SELECT COUNT(*) FROM dev_parking_sessions WHERE exitedAt IS NULL '
                    'AND (priceRuleId IS NOT NULL OR chargedAmount IS NOT NULL OR paymentMethod IS NOT NULL)'
                ).fetchone(), (0,))
                for (entered,) in database.execute('SELECT enteredAt FROM dev_parking_sessions WHERE exitedAt IS NULL'):
                    self.assertLess(datetime.fromisoformat(entered), datetime.fromisoformat(AS_OF))
                groups = database.execute(
                    'SELECT parkingLotId, COUNT(*), SUM(exitedAt IS NOT NULL), SUM(exitedAt IS NULL) '
                    'FROM dev_parking_sessions GROUP BY parkingLotId'
                ).fetchall()
                self.assertEqual(len(groups), 5)
                for lot_id, total, completed, parked in groups:
                    self.assertEqual((total, completed, parked), (50, 30, 20))
                    self.assertEqual(uuid.UUID(lot_id).version, 4)
                    cash, electronic, revenue, count = database.execute(
                        "SELECT SUM(CASE WHEN paymentMethod = 'CASH' THEN chargedAmount ELSE 0 END), "
                        "SUM(CASE WHEN paymentMethod = 'ELECTRONIC' THEN chargedAmount ELSE 0 END), "
                        "SUM(chargedAmount), COUNT(*) FROM dev_parking_sessions WHERE parkingLotId = ? AND exitedAt IS NOT NULL",
                        (lot_id,),
                    ).fetchone()
                    summary = database.execute(
                        'SELECT doanhThu, soLuotXe FROM "SP_ketQuaKinhDoanhBaiDoXe" WHERE baiDoXeId = ? AND kyBaoCao = ?',
                        (lot_id, "2026-11"),
                    ).fetchone()
                    self.assertGreater(cash, 0)
                    self.assertGreater(electronic, 0)
                    self.assertEqual(cash + electronic, revenue)
                    self.assertEqual(summary, (revenue, count))

                finished = database.execute(
                    'SELECT s.enteredAt, s.exitedAt, s.vehicleType, s.chargedAmount, r.loaiPhuongTien, r.loaiNgay, '
                    'r.soGioDau, r.giaGioDau, r.giaGioTiepTheo, f.baiDoXeId, s.parkingLotId, '
                    'f.ngayHieuLuc, f.ngayHetHieuLuc, a.ngayApDung, f.trangThai, a.trangThaiApDung '
                    'FROM dev_parking_sessions s JOIN "SP_chiTietMucThuKhungGia" r ON r.id = s.priceRuleId '
                    'JOIN "SP_khungGiaThuPhi" f ON f.id = r.idKhungGia '
                    'JOIN "SP_apDungKhungGia" a ON a.khungGiaId = f.id AND a.baiDoXeId = s.parkingLotId '
                    'WHERE s.exitedAt IS NOT NULL'
                ).fetchall()
                self.assertEqual(len(finished), 150)
                for (entered, exited, vehicle, charge, rule_vehicle, rule_day, first_hours, first_price, next_price,
                     frame_lot, session_lot, effective_from, effective_until, applied_from, frame_status, application_status) in finished:
                    start = datetime.fromisoformat(entered)
                    end = datetime.fromisoformat(exited)
                    self.assertLess(end, datetime.fromisoformat(AS_OF))
                    self.assertEqual(vehicle, rule_vehicle)
                    self.assertEqual(frame_lot, session_lot)
                    self.assertEqual((frame_status, application_status), ("ACTIVE", "APPLIED"))
                    self.assertLessEqual(datetime.fromisoformat(effective_from), start)
                    self.assertLessEqual(datetime.fromisoformat(applied_from), start)
                    self.assertGreaterEqual(datetime.fromisoformat(effective_until), end)
                    self.assertEqual(rule_day, "WEEKEND" if start.weekday() >= 5 else "WEEKDAY")
                    hours = (end - start).total_seconds() / 3600
                    self.assertEqual(charge, first_price + (hours - first_hours) * next_price)

                self.assertEqual(database.execute(
                    'SELECT doanhThu, soLuotXe FROM "SP_ketQuaKinhDoanhBaiDoXe" WHERE id = ?', ("existing-result",)
                ).fetchone(), (777, 3))
                with self.assertRaises(sqlite3.IntegrityError):
                    database.execute(
                        'INSERT INTO dev_parking_sessions (id, parkingLotId, vehicleType, testPlate, enteredAt, chargedAmount) '
                        'VALUES (?, ?, ?, ?, ?, ?)',
                        (str(uuid.uuid4()), groups[0][0], "MOTORBIKE", "DEMO-INVALID", AS_OF, 1000),
                    )

            repeated = run_seed(database_path)
            self.assertNotEqual(repeated.returncode, 0)
            with closing(sqlite3.connect(database_path)) as database:
                self.assertEqual(database.execute("SELECT COUNT(*) FROM dev_parking_sessions").fetchone(), (250,))

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
