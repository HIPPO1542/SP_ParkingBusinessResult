import sqlite3
import sys
from contextlib import closing
from pathlib import Path


database_path = Path(sys.argv[1]).resolve()
if not database_path.is_file():
    raise SystemExit("Test database does not exist.")

with closing(sqlite3.connect(database_path.as_uri() + "?mode=ro", uri=True)) as database:
    rows = database.execute(
        'SELECT id, daXoa, doanhThu, soLuotXe FROM "SP_ketQuaKinhDoanhBaiDoXe" WHERE baiDoXeId = ? AND kyBaoCao = ?',
        ("00000000-0000-4000-8000-000000000002", "2026-10"),
    ).fetchall()
    if len(rows) != 1 or rows[0][1:] != (1, 3000000.0, 150):
        raise SystemExit("Postman DELETE did not preserve the expected soft-deleted record.")
    print(rows[0][0])
