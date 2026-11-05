"""Back up and migrate the local SQLite database to support soft deletion."""

import sqlite3
import uuid
from contextlib import closing
from pathlib import Path


directory = Path(__file__).resolve().parent
database_path = directory / "parking_business_result.db"
migration_path = directory / "003_soft_delete_business_result.sql"
backup_path = directory / f"parking_business_result.backup-{uuid.uuid4().hex}.db"
table_name = "SP_ketQuaKinhDoanhBaiDoXe"
expected_columns = {
    "id", "baiDoXeId", "kyBaoCao", "doanhThu", "soLuotXe", "idNguoiTao",
    "idNguoiCapNhat", "nguoiCapNhat", "ngayTao", "nguoiTao", "ngayCapNhat", "ghiChu",
}

if not database_path.is_file():
    raise SystemExit("Database not found; initialize migrations 001 and 002 first.")

with closing(sqlite3.connect(database_path.as_uri() + "?mode=rw", uri=True)) as database:
    database.execute("PRAGMA foreign_keys = ON")
    columns = {row[1] for row in database.execute('PRAGMA table_info("SP_ketQuaKinhDoanhBaiDoXe")')}
    if "daXoa" in columns:
        raise SystemExit("The soft-delete column already exists; no migration was applied.")
    if columns != expected_columns:
        raise SystemExit("The existing table differs from migration 002; review it before migration.")

    foreign_keys = database.execute('PRAGMA foreign_key_list("SP_ketQuaKinhDoanhBaiDoXe")').fetchall()
    if len(foreign_keys) != 1 or foreign_keys[0][2:5] != ("DL_KTHT_baiDoXe", "baiDoXeId", "Id"):
        raise SystemExit("The parking-lot foreign key differs from migration 002; no migration was applied.")
    if database.execute("PRAGMA foreign_key_check").fetchall():
        raise SystemExit("Existing foreign-key violations must be resolved before migration.")

    custom_objects = database.execute(
        "SELECT name FROM sqlite_master WHERE tbl_name = ? AND (type = 'trigger' OR (type = 'index' AND sql IS NOT NULL))",
        (table_name,),
    ).fetchall()
    if custom_objects:
        raise SystemExit("The result table has custom indexes or triggers; review them before migration.")
    for (other_table,) in database.execute("SELECT name FROM sqlite_master WHERE type = 'table'").fetchall():
        references = database.execute('SELECT "table" FROM pragma_foreign_key_list(?)', (other_table,)).fetchall()
        if any(target.casefold() == table_name.casefold() for (target,) in references):
            raise SystemExit(f"Table {other_table} references business results; review it before migration.")

    with backup_path.open("xb"):
        pass
    with closing(sqlite3.connect(backup_path)) as backup:
        database.backup(backup)

    try:
        database.executescript(migration_path.read_text(encoding="utf-8"))
    except Exception:
        database.rollback()
        raise

    if database.execute("PRAGMA integrity_check").fetchone() != ("ok",) or database.execute("PRAGMA foreign_key_check").fetchall():
        raise RuntimeError(f"Post-migration database check failed. Backup: {backup_path}")
    print(f"Soft-delete migration complete; backup saved to {backup_path}")
