import sys
from openpyxl import load_workbook

workbook = load_workbook(sys.argv[1], data_only=True)
sheet = workbook.active
assert sheet.max_row == 5
assert sheet['A5'].value == 'Tổng'
assert sheet['A4'].value == 'DEMO-01' and sheet['B4'].value == 'Bai xe gia lap 01'
assert sheet['D5'].value == 30000 and sheet['E5'].value == 1
workbook.close()
print('District export contains only matching parking lot.')
