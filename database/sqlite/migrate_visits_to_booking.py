"""Back up SQLite, move imported visits to the workbook's booking table, and verify them."""
import argparse
import sqlite3
import uuid
from contextlib import closing
from decimal import Decimal, InvalidOperation
from pathlib import Path

DIRECTORY = Path(__file__).resolve().parent


def migrate(path: Path) -> Path:
    path = path.resolve()
    if not path.is_file():
        raise ValueError('Database not found; no changes applied.')
    backup_path = path.with_name(f'{path.stem}.backup-before-booking-{uuid.uuid4().hex}.db')
    with closing(sqlite3.connect(path.as_uri() + '?mode=rw', uri=True)) as database:
        database.execute('PRAGMA foreign_keys = ON')
        tables = {row[0] for row in database.execute("SELECT name FROM sqlite_master WHERE type='table'")}
        if not {'DL_KTHT_baiDoXe', 'parking_visit'}.issubset(tables) or 'SP_datTruocChoDoXe' in tables:
            raise ValueError('Expected old visit table and no booking table; no changes applied.')
        if database.execute('PRAGMA foreign_key_check').fetchall():
            raise ValueError('Existing foreign key errors; no changes applied.')
        for table in tables:
            escaped = table.replace('"', '""')
            if any(row[2].lower() == 'parking_visit' for row in database.execute(f'PRAGMA foreign_key_list("{escaped}")')):
                raise ValueError(f'Table {table} refers to parking_visit; migration needs review.')
        if database.execute("SELECT 1 FROM sqlite_master WHERE type='trigger' AND tbl_name='parking_visit'").fetchone():
            raise ValueError('Visit table has triggers; migration needs review.')
        for (amount,) in database.execute('SELECT collectedAmount FROM parking_visit WHERE collectedAmount IS NOT NULL'):
            try:
                value = Decimal(amount)
                if not value.is_finite() or Decimal(str(float(value))) != value:
                    raise ValueError('A stored amount cannot be represented without loss in the booking amount column.')
            except (InvalidOperation, OverflowError) as exc:
                raise ValueError('Invalid amount in old visit table.') from exc
        with backup_path.open('xb'):
            pass
        with closing(sqlite3.connect(backup_path)) as backup:
            database.backup(backup)
        try:
            database.execute('BEGIN IMMEDIATE')
            sql = (DIRECTORY / '007_booking_table.sql').read_text(encoding='utf-8')
            pending = ''
            for line in sql.splitlines(keepends=True):
                pending += line
                if sqlite3.complete_statement(pending):
                    database.execute(pending)
                    pending = ''
            if pending.strip():
                raise ValueError('Incomplete booking schema statement.')
            before = database.execute('SELECT count(*) FROM parking_visit').fetchone()[0]
            database.execute('''INSERT INTO "SP_datTruocChoDoXe"
                (id,baiDoXeId,loaiXe,thoiGianBatDau,thoiGianKetThuc,trangThaiDatCho,bienSoXe,soLuongChoDat,ngayTao,ngayCapNhat,tongTien)
                SELECT id,parkingLotId,vehicleType,enteredAt,exitedAt,'CONFIRMED',licensePlate,1,createdAt,updatedAt,
                       CASE WHEN collectedAmount IS NULL THEN NULL ELSE CAST(collectedAmount AS REAL) END
                FROM parking_visit''')
            after = database.execute('SELECT count(*) FROM "SP_datTruocChoDoXe"').fetchone()[0]
            mismatched = database.execute('''SELECT id,parkingLotId,vehicleType,enteredAt,exitedAt,licensePlate,createdAt,updatedAt,
                       CASE WHEN collectedAmount IS NULL THEN NULL ELSE CAST(collectedAmount AS REAL) END FROM parking_visit
                EXCEPT SELECT id,baiDoXeId,loaiXe,thoiGianBatDau,thoiGianKetThuc,bienSoXe,ngayTao,ngayCapNhat,tongTien
                FROM "SP_datTruocChoDoXe"''').fetchone()
            if before != after or mismatched or database.execute('PRAGMA foreign_key_check').fetchall():
                raise ValueError('Booking data validation failed.')
            database.execute('DROP TABLE parking_visit')
            database.commit()
        except Exception:
            database.rollback()
            raise
    return backup_path


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--database', type=Path, default=DIRECTORY / 'parking_business_result.db')
    args = parser.parse_args()
    try:
        backup_path = migrate(args.database)
    except (sqlite3.Error, OSError, ValueError) as error:
        raise SystemExit(str(error)) from error
    print(f'Booking table migrated successfully. Backup: {backup_path}')
