"""Back up and add the parking_visit table without altering existing records."""
import argparse
import sqlite3
import uuid
from contextlib import closing
from pathlib import Path

DIRECTORY = Path(__file__).resolve().parent


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--database', type=Path, default=DIRECTORY / 'parking_business_result.db')
    path = parser.parse_args().database.resolve()
    if not path.is_file():
        raise SystemExit('Database not found; no changes applied.')
    backup_path = path.with_name(f'{path.stem}.backup-visits-{uuid.uuid4().hex}.db')
    with closing(sqlite3.connect(path.as_uri() + '?mode=rw', uri=True)) as database:
        database.execute('PRAGMA foreign_keys = ON')
        tables = {row[0] for row in database.execute("SELECT name FROM sqlite_master WHERE type='table'")}
        if 'DL_KTHT_baiDoXe' not in tables or 'parking_visit' in tables:
            raise SystemExit('Missing parking lots or visit table already exists; no changes applied.')
        if database.execute('PRAGMA foreign_key_check').fetchall():
            raise SystemExit('Existing foreign key errors; no changes applied.')
        with backup_path.open('xb'):
            pass
        with closing(sqlite3.connect(backup_path)) as backup:
            database.backup(backup)
        try:
            database.execute('BEGIN IMMEDIATE')
            statements = (DIRECTORY / '006_parking_visits.sql').read_text(encoding='utf-8')
            pending = ''
            for line in statements.splitlines(keepends=True):
                pending += line
                if sqlite3.complete_statement(pending):
                    database.execute(pending)
                    pending = ''
            if pending.strip():
                raise ValueError('Incomplete migration statement.')
            if database.execute('PRAGMA foreign_key_check').fetchall():
                raise ValueError('Foreign-key validation failed.')
            database.commit()
        except Exception:
            database.rollback()
            raise
    print(f'Created parking_visit; backup: {backup_path}')


if __name__ == '__main__':
    main()
