"""Adopt a matching existing database without replaying CREATE TABLE or touching business rows."""
import argparse
import re
import sqlite3
from datetime import datetime
from pathlib import Path


def signature(connection):
    schema = {}
    for name, sql in connection.execute("SELECT name,sql FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' AND name NOT LIKE '__EF%'"):
        quoted = '"' + name.replace('"', '""') + '"'
        columns = list(connection.execute(f'PRAGMA table_info({quoted})'))
        foreign_keys = list(connection.execute(f'PRAGMA foreign_key_list({quoted})'))
        indexes = []
        for index in connection.execute(f'PRAGMA index_list({quoted})'):
            index_name = '"' + index[1].replace('"', '""') + '"'
            definition = connection.execute('SELECT sql FROM sqlite_master WHERE name=?', (index[1],)).fetchone()
            where = (definition[0].split(' WHERE ', 1)[-1] if definition and definition[0] and ' WHERE ' in definition[0] else '')
            indexes.append((index[2], tuple(row[2] for row in connection.execute(f'PRAGMA index_info({index_name})')), re.sub(r'\s+', ' ', where.strip())))
        schema[name] = (columns, foreign_keys, sorted(indexes), re.sub(r'\s+', ' ', sql.strip()))
    return schema


parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--database', required=True, type=Path)
parser.add_argument('--template', required=True, type=Path, help='Empty database created with the current EF initial migration')
args = parser.parse_args()
database_path = args.database.resolve(strict=True)
template_path = args.template.resolve(strict=True)
if database_path == template_path:
    raise SystemExit('Database and template must be different files.')

with sqlite3.connect(database_path.as_uri() + '?mode=rw', uri=True) as database, sqlite3.connect(template_path.as_uri() + '?mode=ro', uri=True) as template:
    if signature(database) != signature(template):
        raise SystemExit('Schema differs from the EF baseline. No changes were made; review schema before adoption.')
    migration = template.execute('SELECT MigrationId,ProductVersion FROM __EFMigrationsHistory').fetchall()
    migration_files = list((Path(__file__).resolve().parents[2] / 'Backend/ParkingBusinessResult.Infrastructure/Persistence/Migrations').glob('*_InitialParkingSchema.cs'))
    if len(migration) != 1 or len(migration_files) != 1 or migration[0][0] != migration_files[0].stem:
        raise SystemExit('Template must contain exactly the initial EF migration.')
    backup_path = database_path.with_name(database_path.stem + '.before-ef-' + datetime.now().strftime('%Y%m%d%H%M%S%f') + '.db')
    with sqlite3.connect(backup_path) as backup:
        database.backup(backup)
    database.execute('BEGIN IMMEDIATE')
    try:
        if signature(database) != signature(template):
            raise ValueError('Schema changed while adopting EF baseline.')
        database.execute('CREATE TABLE IF NOT EXISTS __EFMigrationsHistory (MigrationId TEXT NOT NULL PRIMARY KEY, ProductVersion TEXT NOT NULL)')
        existing = database.execute('SELECT MigrationId,ProductVersion FROM __EFMigrationsHistory').fetchall()
        if existing and existing != migration:
            raise ValueError('Unexpected migration history. Adoption was rolled back.')
        if not existing:
            database.executemany('INSERT INTO __EFMigrationsHistory VALUES (?,?)', migration)
        database.commit()
    except Exception:
        database.rollback()
        raise
    print(f'EF baseline adopted. Business rows unchanged. Backup: {backup_path}')
