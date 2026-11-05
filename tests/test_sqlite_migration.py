import sqlite3
import subprocess
import sys
import tempfile
import unittest
from contextlib import closing
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
INITIAL = (ROOT / "database/sqlite/001_create_business_result.sql").read_text(encoding="utf-8")
MIGRATION = (ROOT / "database/sqlite/002_business_reference_tables.sql").read_text(encoding="utf-8")


class BusinessReferenceMigrationTests(unittest.TestCase):
    def setUp(self):
        self.database = sqlite3.connect(":memory:")
        self.database.execute("PRAGMA foreign_keys=ON")
        self.database.executescript(INITIAL)

    def tearDown(self):
        self.database.close()

    def test_reference_tables_and_foreign_keys(self):
        self.database.executescript(MIGRATION)
        self.assertEqual(self.database.execute("PRAGMA foreign_key_check").fetchall(), [])
        self.database.execute(
            'INSERT INTO "SP_donViKinhDoanhQuanLy" (id, tenDonVi, diaChi, nguoiLienHe) VALUES (?, ?, ?, ?)',
            ("unit", "Unit", "Address", "Contact"),
        )
        self.database.execute(
            'INSERT INTO "DL_KTHT_baiDoXe" (Id, quyHoachId, donViQuanLyId, geo, dienTichThucTe) VALUES (?, ?, ?, ?, ?)',
            ("lot", "plan", "unit", "{}", 10),
        )
        self.database.execute(
            'INSERT INTO "SP_ketQuaKinhDoanhBaiDoXe" (id, baiDoXeId, kyBaoCao, doanhThu, soLuotXe) VALUES (?, ?, ?, ?, ?)',
            ("result", "lot", "2026-10", 100, 2),
        )
        with self.assertRaises(sqlite3.IntegrityError):
            self.database.execute(
                'INSERT INTO "SP_ketQuaKinhDoanhBaiDoXe" (id, baiDoXeId, kyBaoCao, doanhThu, soLuotXe) VALUES (?, ?, ?, ?, ?)',
                ("other", "missing-lot", "2026-10", 100, 2),
            )
        with self.assertRaises(sqlite3.IntegrityError):
            self.database.execute('DELETE FROM "DL_KTHT_baiDoXe" WHERE Id = ?', ("lot",))

    def test_orphaned_existing_result_rolls_back_migration(self):
        self.database.execute(
            'INSERT INTO "SP_ketQuaKinhDoanhBaiDoXe" (id, baiDoXeId, kyBaoCao, doanhThu, soLuotXe) VALUES (?, ?, ?, ?, ?)',
            ("result", "missing-lot", "2026-10", 100, 2),
        )
        self.database.commit()
        with self.assertRaises(sqlite3.IntegrityError):
            self.database.executescript(MIGRATION)
        self.database.rollback()
        self.assertEqual(self.database.execute('SELECT COUNT(*) FROM "SP_ketQuaKinhDoanhBaiDoXe"').fetchone(), (1,))
        self.assertIsNone(self.database.execute(
            "SELECT name FROM sqlite_master WHERE type='table' AND name='DL_KTHT_baiDoXe'"
        ).fetchone())

    def test_migration_runner_creates_backup(self):
        with tempfile.TemporaryDirectory() as temporary_directory:
            directory = Path(temporary_directory)
            source = ROOT / "database/sqlite"
            for filename in ("002_business_reference_tables.sql", "apply_business_reference_migration.py"):
                (directory / filename).write_bytes((source / filename).read_bytes())
            with closing(sqlite3.connect(directory / "parking_business_result.db")) as temporary_database:
                temporary_database.executescript(INITIAL)
            completed = subprocess.run(
                [sys.executable, str(directory / "apply_business_reference_migration.py")],
                capture_output=True,
                text=True,
                check=False,
            )
            self.assertEqual(completed.returncode, 0, completed.stderr)
            self.assertEqual(len(list(directory.glob("parking_business_result.backup-*.db"))), 1)
            with closing(sqlite3.connect(directory / "parking_business_result.db")) as migrated:
                self.assertEqual(migrated.execute("PRAGMA foreign_key_check").fetchall(), [])
                self.assertTrue(migrated.execute("PRAGMA foreign_key_list(SP_ketQuaKinhDoanhBaiDoXe)").fetchall())


if __name__ == "__main__":
    unittest.main()
