import sqlite3
import subprocess
import sys
import tempfile
from contextlib import closing
from pathlib import Path

root = Path(__file__).resolve().parents[1]
with tempfile.TemporaryDirectory(prefix='parking-visit-migration-') as directory:
    path = Path(directory) / 'data.db'
    with closing(sqlite3.connect(path)) as db:
        db.execute('CREATE TABLE "DL_KTHT_baiDoXe" ("Id" TEXT PRIMARY KEY)')
        db.execute('INSERT INTO "DL_KTHT_baiDoXe" VALUES (?)', ('existing-lot',))
        db.commit()
    command = [sys.executable, str(root / 'database/sqlite/apply_parking_visit_migration.py'), '--database', str(path)]
    subprocess.run(command, check=True, capture_output=True, text=True)
    backups = list(path.parent.glob('data.backup-visits-*.db'))
    assert len(backups) == 1
    with closing(sqlite3.connect(backups[0])) as backup:
        assert backup.execute('SELECT count(*) FROM "DL_KTHT_baiDoXe"').fetchone()[0] == 1
        assert backup.execute("SELECT count(*) FROM sqlite_master WHERE name='parking_visit'").fetchone()[0] == 0
    with closing(sqlite3.connect(path)) as db:
        assert db.execute('SELECT count(*) FROM "DL_KTHT_baiDoXe"').fetchone()[0] == 1
        assert db.execute("SELECT count(*) FROM sqlite_master WHERE name='parking_visit'").fetchone()[0] == 1
    repeated = subprocess.run(command, capture_output=True, text=True)
    assert repeated.returncode != 0
    assert len(list(path.parent.glob('data.backup-visits-*.db'))) == 1
print('Parking visit migration backup, data preservation and repeat-run checks passed.')
