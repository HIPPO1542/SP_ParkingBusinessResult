import sys
from openpyxl import load_workbook
from pypdf import PdfReader

excel_path, pdf_path = sys.argv[1:]
workbook = load_workbook(excel_path, data_only=False)
sheet = workbook.active
assert sheet['A1'].value == '=safe test' and sheet['A1'].data_type == 's'
assert sheet.max_row == 6
assert sheet['A6'].value == 'Tổng'
assert sheet['C6'].value == 35000 and sheet['D6'].value == 2 and sheet['E6'].value == 17500
workbook.close()
text = '\n'.join(page.extract_text() for page in PdfReader(pdf_path).pages)
assert '35,000.00' in text and '17,500.00' in text and 'Tổng' in text
print('Excel and PDF exports match imported visit totals.')
