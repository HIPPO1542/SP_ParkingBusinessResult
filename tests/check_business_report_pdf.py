import sys
from pypdf import PdfReader

path, expected_count, expected_revenue = sys.argv[1:]
reader = PdfReader(path)
text = '\n'.join(page.extract_text() for page in reader.pages)
assert 'Kỳ báo cáo: 2026-11' in text
assert 'Tổng' in text
assert expected_revenue in text
assert str(int(expected_count) * 30) in text
assert '18,500.00' in text
assert text.count('2026-11') == int(expected_count) + 2
print('PDF report content verified.')
