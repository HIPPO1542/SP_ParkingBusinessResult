import sqlite3
import sys
import csv
import importlib.util
from datetime import datetime
from pathlib import Path

from openpyxl import Workbook

HEADERS = ['Mã bãi', 'Biển số', 'Loại xe', 'Giờ vào', 'Giờ ra', 'Số tiền đã thu (VND)', 'Phương thức thanh toán']


def write(path, rows):
    workbook = Workbook()
    sheet = workbook.active
    sheet.append(HEADERS)
    for row in rows:
        sheet.append(row)
    workbook.save(path)


def make_fixtures(directory):
    write(directory / 'first.xlsx', [
        ['DEMO-01', '59A-1', 'Ô tô', '2026-11-04 23:30', None, None, None],
        ['DEMO-02', '59B-2', 'MOTORBIKE', datetime(2026, 11, 5, 8, 0), datetime(2026, 11, 5, 9, 0), 5000, 'Tiền mặt'],
        ['DEMO-02', '59Z-9', 'Xe máy', '2026-10-05 08:00', '2026-10-05 09:00', 10000, 'Tiền mặt'],
        ['NO-SUCH-LOT', '59C-3', 'Xe máy', '2026-11-05 08:00', None, None, None],
        ['DEMO-01', '59D-4', 'Xe máy', '2026-11-05 10:00', '2026-11-05 09:00', 0, None],
    ])
    write(directory / 'update.xlsx', [
        ['DEMO-01', '59a-1', 'Ô tô', '2026-11-04 23:30', '2026-11-05 01:00', 30000, 'Điện tử'],
    ])
    write(directory / 'scoped.xlsx', [
        ['DEMO-01', '59E-5', 'Xe máy', '2026-11-05 08:00', None, None, None],
        ['DEMO-02', '59F-6', 'Xe máy', '2026-11-05 08:00', None, None, None],
        ['DEMO-01', '59G-7', 'Xe máy', '05/11/2026 08:00', '05/11/2026 09:00', None, None],
    ])
    write(directory / 'blank_update.xlsx', [
        ['DEMO-01', '59A-1', 'Ô tô', '2026-11-04 23:30', None, None, None],
    ])
    write(directory / 'fractional.xlsx', [
        ['DEMO-02', '59H-8', 'MOTORBIKE', '2026-11-05 08:00', '2026-11-05 09:30', 7000, 'Tiền mặt'],
    ])
    write(directory / 'unclassified.xlsx', [
        ['DEMO-02', '59J-8', 'MOTORBIKE', '2026-11-05 10:00', '2026-11-05 11:00', 9000, 'Khác'],
    ])
    write(directory / 'unknown_district.xlsx', [
        ['DEMO-03', '59M-1', 'MOTORBIKE', '2026-11-05 10:00', '2026-11-05 11:00', 2000, 'Tiền mặt'],
    ])
    (directory / 'invalid.xlsx').write_bytes(b'not an Excel workbook')
    with (directory / 'visit.csv').open('w', encoding='utf-8-sig', newline='') as file:
        writer = csv.writer(file)
        writer.writerow(HEADERS)
        writer.writerow(['DEMO-02', '59K-9', 'MOTORBIKE', '2026-11-05 10:00', '2026-11-05 11:00', '5000', 'Tiền mặt'])
    if importlib.util.find_spec('xlwt'):
        import xlwt
        workbook = xlwt.Workbook()
        sheet = workbook.add_sheet('Luot xe')
        for col, value in enumerate(HEADERS):
            sheet.write(0, col, value)
        for col, value in enumerate(['DEMO-02', '59L-0', 'MOTORBIKE', '2026-11-05 10:00', '2026-11-05 11:00', 5000, 'Tiền mặt']):
            sheet.write(1, col, value)
        workbook.save(str(directory / 'visit.xls'))


def check(database_path):
    with sqlite3.connect(database_path) as database:
        rows = database.execute('SELECT licensePlate, enteredAt, exitedAt, collectedAmount, paymentMethod FROM parking_visit ORDER BY licensePlate').fetchall()
        assert len(rows) == 5, rows
        assert rows[0] == ('59A-1', '2026-11-04 23:30:00', '2026-11-05 01:00:00', '30000', 'Điện tử'), rows[0]
        assert rows[1][0] == '59B-2' and rows[1][3] == '5000', rows[1]
        assert rows[2][0] == '59E-5' and rows[2][2] is None, rows[2]
        assert rows[3][0] == '59G-7' and rows[3][3] is None, rows[3]
        assert rows[4][0] == '59Z-9' and rows[4][3] == '10000', rows[4]
        assert not database.execute('PRAGMA foreign_key_check').fetchall()
    print('Imported visits verified directly in SQLite.')


if sys.argv[1] == 'make':
    make_fixtures(Path(sys.argv[2]))
elif sys.argv[1] == 'addresses':
    with sqlite3.connect(sys.argv[2]) as database:
        database.execute('UPDATE DL_KTHT_baiDoXe SET diaChi=? WHERE ma=?', ('1 Đường Mẫu, Quận 1, Thành phố Mẫu', 'DEMO-01'))
        database.execute('UPDATE DL_KTHT_baiDoXe SET diaChi=? WHERE ma=?', ('2 Đường Mẫu, Quận 2, Thành phố Mẫu', 'DEMO-02'))
else:
    check(Path(sys.argv[2]))
