import sqlite3
import uuid
from pathlib import Path


database = Path(__file__).resolve().parents[1] / "database/sqlite/parking_business_result.db"
if not database.is_file():
    raise SystemExit("SQLite database has not been created.")

with sqlite3.connect(database) as connection:
    assert connection.execute("PRAGMA integrity_check").fetchone() == ("ok",)
    connection.execute("BEGIN")
    try:
        record_id = str(uuid.uuid4())
        parking_lot_id = str(uuid.uuid4())
        insert_sql = '''INSERT INTO "SP_ketQuaKinhDoanhBaiDoXe"
            ("id", "baiDoXeId", "kyBaoCao", "doanhThu", "soLuotXe")
            VALUES (?, ?, ?, ?, ?)'''
        connection.execute(insert_sql, (record_id, parking_lot_id, "2026-10", 100.0, 2))
        assert connection.execute(
            'SELECT "doanhThu", "soLuotXe" FROM "SP_ketQuaKinhDoanhBaiDoXe" WHERE "id" = ?',
            (record_id,),
        ).fetchone() == (100.0, 2)

        for values in [
            (str(uuid.uuid4()), parking_lot_id, "2026-10", 100.0, 2),
            (str(uuid.uuid4()), parking_lot_id, "2026-11", -1.0, 2),
            (str(uuid.uuid4()), parking_lot_id, "2026-12", 100.0, -1),
        ]:
            try:
                connection.execute(insert_sql, values)
            except sqlite3.IntegrityError:
                continue
            raise AssertionError("A duplicate or negative value was accepted")
    finally:
        connection.rollback()

    assert connection.execute(
        'SELECT COUNT(*) FROM "SP_ketQuaKinhDoanhBaiDoXe" WHERE "id" = ?', (record_id,)
    ).fetchone() == (0,)

print("SQLite integrity, insert, uniqueness, validation and rollback checks passed.")
