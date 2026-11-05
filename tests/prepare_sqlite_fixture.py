import sqlite3
import sys
from pathlib import Path


root = Path(__file__).resolve().parents[1]
database_path, parking_lot_id = sys.argv[1:]
with sqlite3.connect(database_path) as database:
    for name in ("001_create_business_result.sql", "002_business_reference_tables.sql"):
        database.executescript((root / "database/sqlite" / name).read_text(encoding="utf-8"))
    database.execute(
        'INSERT INTO "SP_donViKinhDoanhQuanLy" (id, tenDonVi, diaChi, nguoiLienHe) VALUES (?, ?, ?, ?)',
        ("unit", "Unit", "Address", "Contact"),
    )
    database.execute(
        'INSERT INTO "DL_KTHT_baiDoXe" (Id, quyHoachId, donViQuanLyId, geo, dienTichThucTe) VALUES (?, ?, ?, ?, ?)',
        (parking_lot_id, "plan", "unit", "{}", 1),
    )
