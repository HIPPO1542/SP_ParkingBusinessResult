import sqlite3
import subprocess
import sys
import tempfile
import unittest
from contextlib import closing
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
SQLITE = ROOT / "database/sqlite"
INITIAL = (SQLITE / "001_create_business_result.sql").read_text(encoding="utf-8")
REFERENCES = (SQLITE / "002_business_reference_tables.sql").read_text(encoding="utf-8")
SOFT_DELETE = (SQLITE / "003_soft_delete_business_result.sql").read_text(encoding="utf-8")


def initialize(database):
    database.executescript(INITIAL)
    database.executescript(REFERENCES)
    database.execute('INSERT INTO "SP_donViKinhDoanhQuanLy" (id, tenDonVi, diaChi, nguoiLienHe) VALUES (?, ?, ?, ?)',
                     ("unit", "Unit", "Address", "Contact"))
    database.execute('INSERT INTO "DL_KTHT_baiDoXe" (Id, quyHoachId, donViQuanLyId, geo, dienTichThucTe) VALUES (?, ?, ?, ?, ?)',
                     ("lot", "plan", "unit", "{}", 1))
    database.execute('INSERT INTO "SP_ketQuaKinhDoanhBaiDoXe" (id, baiDoXeId, kyBaoCao, doanhThu, soLuotXe, nguoiTao, ghiChu) VALUES (?, ?, ?, ?, ?, ?, ?)',
                     ("existing", "lot", "2026-10", 123, 4, "tester", "original"))
    database.commit()


class SoftDeleteMigrationTests(unittest.TestCase):
    def test_preserves_records_and_reuses_period_only_after_delete(self):
        with closing(sqlite3.connect(":memory:")) as database:
            initialize(database)
            database.executescript(SOFT_DELETE)
            self.assertEqual(database.execute('SELECT id, doanhThu, soLuotXe, nguoiTao, ghiChu, daXoa FROM "SP_ketQuaKinhDoanhBaiDoXe"').fetchone(),
                             ("existing", 123, 4, "tester", "original", 0))
            duplicate = 'INSERT INTO "SP_ketQuaKinhDoanhBaiDoXe" (id, baiDoXeId, kyBaoCao, doanhThu, soLuotXe) VALUES (?, ?, ?, ?, ?)'
            with self.assertRaises(sqlite3.IntegrityError):
                database.execute(duplicate, ("duplicate", "lot", "2026-10", 200, 2))
            database.execute('UPDATE "SP_ketQuaKinhDoanhBaiDoXe" SET daXoa = 1 WHERE id = ?', ("existing",))
            database.execute(duplicate, ("replacement", "lot", "2026-10", 200, 2))
            self.assertEqual(database.execute('SELECT COUNT(*) FROM "SP_ketQuaKinhDoanhBaiDoXe"').fetchone(), (2,))
            self.assertEqual(database.execute("PRAGMA foreign_key_check").fetchall(), [])

    def test_runner_backs_up_and_refuses_second_application(self):
        with tempfile.TemporaryDirectory() as temporary_directory:
            directory = Path(temporary_directory)
            for filename in ("003_soft_delete_business_result.sql", "apply_soft_delete_migration.py"):
                (directory / filename).write_bytes((SQLITE / filename).read_bytes())
            database_path = directory / "parking_business_result.db"
            with closing(sqlite3.connect(database_path)) as database:
                initialize(database)
            command = [sys.executable, str(directory / "apply_soft_delete_migration.py")]
            first = subprocess.run(command, capture_output=True, text=True, encoding="utf-8", check=False)
            self.assertEqual(first.returncode, 0, first.stderr)
            backups = list(directory.glob("parking_business_result.backup-*.db"))
            self.assertEqual(len(backups), 1)
            with closing(sqlite3.connect(backups[0])) as backup:
                self.assertEqual(backup.execute('SELECT COUNT(*) FROM "SP_ketQuaKinhDoanhBaiDoXe"').fetchone(), (1,))
                self.assertNotIn("daXoa", [row[1] for row in backup.execute('PRAGMA table_info("SP_ketQuaKinhDoanhBaiDoXe")')])
            repeated = subprocess.run(command, capture_output=True, text=True, encoding="utf-8", check=False)
            self.assertNotEqual(repeated.returncode, 0)
            with closing(sqlite3.connect(database_path)) as database:
                self.assertEqual(database.execute('SELECT id, daXoa FROM "SP_ketQuaKinhDoanhBaiDoXe"').fetchone(), ("existing", 0))

    def test_runner_rejects_inbound_foreign_key_before_changes(self):
        with tempfile.TemporaryDirectory() as temporary_directory:
            directory = Path(temporary_directory)
            for filename in ("003_soft_delete_business_result.sql", "apply_soft_delete_migration.py"):
                (directory / filename).write_bytes((SQLITE / filename).read_bytes())
            database_path = directory / "parking_business_result.db"
            with closing(sqlite3.connect(database_path)) as database:
                initialize(database)
                database.execute('CREATE TABLE child (id TEXT PRIMARY KEY, result_id TEXT REFERENCES "SP_ketQuaKinhDoanhBaiDoXe"(id) ON DELETE CASCADE)')
                database.execute("INSERT INTO child (id, result_id) VALUES ('child', 'existing')")
                database.commit()
            process = subprocess.run([sys.executable, str(directory / "apply_soft_delete_migration.py")],
                                     capture_output=True, text=True, encoding="utf-8", check=False)
            self.assertNotEqual(process.returncode, 0)
            with closing(sqlite3.connect(database_path)) as database:
                self.assertEqual(database.execute("SELECT COUNT(*) FROM child").fetchone(), (1,))
                self.assertEqual(database.execute('SELECT COUNT(*) FROM "SP_ketQuaKinhDoanhBaiDoXe"').fetchone(), (1,))


if __name__ == "__main__":
    unittest.main()
