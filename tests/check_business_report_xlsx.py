import sys
from decimal import Decimal

from openpyxl import load_workbook

path, expected_count, expected_revenue = sys.argv[1:]
workbook = load_workbook(path, data_only=False)
sheet = workbook.active
assert sheet['A1'].value == '=test formula is text'
assert sheet['A1'].data_type == 's'
assert sheet['A2'].value == 'Kỳ báo cáo'
assert sheet['B2'].value == '2026-11'
assert sheet.max_row == int(expected_count) + 4
assert sheet.cell(sheet.max_row, 1).value == 'Tổng'
assert sheet['A3'].value == 'Mã bãi đỗ xe' and sheet['B3'].value == 'Tên bãi đỗ xe'
assert all(str(sheet.cell(row, 1).value).startswith('DEMO-') and str(sheet.cell(row, 2).value).startswith('Bai xe gia lap') for row in range(4, sheet.max_row))
assert Decimal(str(sheet.cell(sheet.max_row, 4).value)) == Decimal(expected_revenue)
assert sheet.cell(sheet.max_row, 5).value == int(expected_count) * 30
assert sheet.cell(sheet.max_row, 6).value == 18500
workbook.close()
print('Excel report content verified.')
