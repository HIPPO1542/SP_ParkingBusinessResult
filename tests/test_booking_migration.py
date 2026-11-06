import sqlite3
import subprocess
import sys
import tempfile
from contextlib import closing
from pathlib import Path

root = Path(__file__).resolve().parents[1]
python = sys.executable
script = root / 'database/sqlite/migrate_visits_to_booking.py'
expected_columns = ['id', 'baiDoXeId', 'loaiXe', 'thoiGianBatDau', 'thoiGianKetThuc', 'trangThaiDatCho',
                    'bienSoXe', 'soLuongChoDat', 'idNguoiTao', 'idNguoiCapNhat', 'nguoiCapNhat', 'ngayTao',
                    'nguoiTao', 'ngayCapNhat', 'ghiChu', 'tongTien']

with tempfile.TemporaryDirectory(prefix='booking-migration-') as directory:
    db_path = Path(directory) / 'data.db'
    with closing(sqlite3.connect(db_path)) as db, db:
        db.execute('CREATE TABLE "DL_KTHT_baiDoXe" ("Id" TEXT PRIMARY KEY)')
        db.execute('INSERT INTO "DL_KTHT_baiDoXe" VALUES (?)', ('lot-1',))
        db.executescript((root / 'database/sqlite/006_parking_visits.sql').read_text(encoding='utf-8'))
        db.executemany('INSERT INTO parking_visit VALUES (?,?,?,?,?,?,?,?,?,?)', [
            ('visit-1', 'lot-1', '59A', 'CAR', '2026-11-05 08:00:00', '2026-11-05 10:00:00', '30000', 'Tiền mặt', 'created', 'updated'),
            ('visit-2', 'lot-1', '59B', 'MOTORBIKE', '2026-11-05 09:00:00', None, None, None, 'created', 'updated'),
        ])
    subprocess.run([python, str(script), '--database', str(db_path)], check=True, capture_output=True, text=True)
    backups = list(Path(directory).glob('data.backup-before-booking-*.db'))
    assert len(backups) == 1
    with closing(sqlite3.connect(backups[0])) as backup:
        assert backup.execute('SELECT count(*) FROM parking_visit').fetchone()[0] == 2
        assert backup.execute('SELECT paymentMethod FROM parking_visit WHERE id=?', ('visit-1',)).fetchone()[0] == 'Tiền mặt'
    with closing(sqlite3.connect(db_path)) as db:
        columns = [row[1] for row in db.execute('PRAGMA table_info("SP_datTruocChoDoXe")')]
        assert columns == expected_columns, columns
        rows = db.execute('SELECT id,baiDoXeId,bienSoXe,thoiGianKetThuc,tongTien,soLuongChoDat FROM SP_datTruocChoDoXe ORDER BY id').fetchall()
        assert rows == [('visit-1', 'lot-1', '59A', '2026-11-05 10:00:00', 30000, 1),
                        ('visit-2', 'lot-1', '59B', None, None, 1)], rows
        assert db.execute("SELECT count(*) FROM sqlite_master WHERE name='parking_visit'").fetchone()[0] == 0
        assert not db.execute('PRAGMA foreign_key_check').fetchall()
    repeated = subprocess.run([python, str(script), '--database', str(db_path)], capture_output=True, text=True)
    assert repeated.returncode != 0
    assert len(list(Path(directory).glob('data.backup-before-booking-*.db'))) == 1

print('Booking migration preserves visits and creates a backup; repeat migration is blocked.')
