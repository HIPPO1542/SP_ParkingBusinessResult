"""Insert optional Postman demo data into the local development SQLite database."""

import sqlite3
from pathlib import Path


root = Path(__file__).resolve().parents[1]
database_path = root / "database/sqlite/parking_business_result.db"
seed_path = root / "postman/seed-demo-parking-lot.sql"

if not database_path.is_file():
    raise SystemExit("SQLite database not found. Initialize it using the project README first.")

connection = sqlite3.connect(database_path.as_uri() + "?mode=rw", uri=True)
try:
    try:
        connection.executescript(seed_path.read_text(encoding="utf-8"))
    except sqlite3.Error as error:
        connection.rollback()
        raise SystemExit(f"Unable to insert demo data; no changes were committed: {error}") from error
finally:
    connection.close()

print("Demo parking lot created: 00000000-0000-4000-8000-000000000002")
