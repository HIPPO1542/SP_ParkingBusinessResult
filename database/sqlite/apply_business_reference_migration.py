import sqlite3
import uuid
from contextlib import closing
from pathlib import Path


directory = Path(__file__).resolve().parent
database_path = directory / "parking_business_result.db"
migration_path = directory / "002_business_reference_tables.sql"
backup_path = directory / f"parking_business_result.backup-{uuid.uuid4().hex}.db"

if not database_path.is_file():
    raise SystemExit("Database not found; initialize 001 before running this migration.")

database = sqlite3.connect(database_path.as_uri() + "?mode=rw", uri=True)
try:
    existing = database.execute(
        "SELECT name FROM sqlite_master WHERE type = 'table' AND name IN (?, ?)",
        ("SP_donViKinhDoanhQuanLy", "DL_KTHT_baiDoXe"),
    ).fetchall()
    if existing:
        raise SystemExit("Reference tables already exist; migration was not applied.")
    count = database.execute('SELECT COUNT(*) FROM "SP_ketQuaKinhDoanhBaiDoXe"').fetchone()[0]
    if count:
        raise SystemExit("Existing results require a separate migration with parking-lot data; no changes were applied.")
    custom_objects = database.execute(
        "SELECT name FROM sqlite_master WHERE tbl_name = ? AND (type = 'trigger' OR (type = 'index' AND sql IS NOT NULL))",
        ("SP_ketQuaKinhDoanhBaiDoXe",),
    ).fetchall()
    if custom_objects:
        raise SystemExit("The result table has custom indexes or triggers; review them before migration.")
    table_names = database.execute("SELECT name FROM sqlite_master WHERE type = 'table'").fetchall()
    for (table_name,) in table_names:
        references = database.execute("SELECT \"table\" FROM pragma_foreign_key_list(?)", (table_name,)).fetchall()
        if any(target.casefold() == "SP_ketQuaKinhDoanhBaiDoXe".casefold() for (target,) in references):
            raise SystemExit(f"Table {table_name} references business results; review it before migration.")

    with backup_path.open("xb"):
        pass
    with closing(sqlite3.connect(backup_path)) as backup:
        database.backup(backup)

    try:
        database.executescript(migration_path.read_text(encoding="utf-8"))
    except Exception:
        database.rollback()
        raise

    if database.execute("PRAGMA integrity_check").fetchone() != ("ok",):
        raise RuntimeError(f"Integrity check failed. Backup: {backup_path}")
    if database.execute("PRAGMA foreign_key_check").fetchall():
        raise RuntimeError(f"Foreign key check failed. Backup: {backup_path}")
    print(f"Migration complete; backup saved to {backup_path}")
finally:
    database.close()
