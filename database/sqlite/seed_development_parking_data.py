"""Add simulated parking lots, price references and independent business totals.

This script does not create or modify parking bookings or legacy visit data.
"""

import argparse
import json
import sqlite3
import uuid
from contextlib import closing
from datetime import datetime, timedelta, timezone
from pathlib import Path


DIRECTORY = Path(__file__).resolve().parent
LOCAL_ZONE = timezone(timedelta(hours=7))
CREATOR = "du_lieu_gia_lap"
NEW_TABLES = (
    "SP_khungGiaThuPhi", "SP_chiTietMucThuKhungGia", "SP_apDungKhungGia",
)
RATES = {
    ("MOTORBIKE", "WEEKDAY"): (5000, 2000),
    ("MOTORBIKE", "WEEKEND"): (7000, 3000),
    ("CAR_9_SEATS", "WEEKDAY"): (20000, 10000),
    ("CAR_9_SEATS", "WEEKEND"): (25000, 12000),
}


def new_id():
    return str(uuid.uuid4())


def execute_schema(database, schema_path):
    statement = ""
    for line in schema_path.read_text(encoding="utf-8").splitlines(keepends=True):
        statement += line
        if sqlite3.complete_statement(statement):
            database.execute(statement)
            statement = ""
    if statement.strip():
        raise ValueError(f"Incomplete SQL statement in {schema_path.name}")


def seed(database, as_of):
    completed_day = (as_of - timedelta(days=1)).date()
    day_type = "WEEKEND" if completed_day.weekday() >= 5 else "WEEKDAY"
    effective_from = (as_of - timedelta(days=365)).isoformat()
    effective_until = (as_of + timedelta(days=365)).isoformat()
    created_at = as_of.isoformat()

    for lot_number in range(1, 6):
        unit_id = new_id()
        lot_id = new_id()
        frame_id = new_id()
        note = "Synthetic development data; coordinates and planning ID are placeholders."
        x = 106.0 + lot_number * 0.01
        coordinates = [[[x, 10.0], [x + 0.001, 10.0], [x + 0.001, 10.001], [x, 10.0]]]
        geometry = json.dumps({"type": "MultiPolygon", "coordinates": [coordinates]}, separators=(",", ":"))
        database.execute(
            'INSERT INTO "SP_donViKinhDoanhQuanLy" (id, tenDonVi, diaChi, nguoiLienHe, ngayTao, nguoiTao, ghiChu) VALUES (?, ?, ?, ?, ?, ?, ?)',
            (unit_id, f"Don vi gia lap {lot_number:02d}", "Dia chi gia lap", "Lien he gia lap", created_at, CREATOR, note),
        )
        database.execute(
            'INSERT INTO "DL_KTHT_baiDoXe" (Id, quyHoachId, donViQuanLyId, ten, ma, geo, diaChi, tongSucChua, '
            'sucChuaHienHuuXeMoto, sucChuaHienHuuXeOto, sucChuaHienHuuXeLon, dienTichThucTe, ngayTao, nguoiTao, ghiChu) '
            'VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)',
            (lot_id, new_id(), unit_id, f"Bai xe gia lap {lot_number:02d}",
             f"DEMO-{lot_number:02d}", geometry, "Dia chi gia lap", 60, 30, 25, 5, 1000, created_at, CREATOR, note),
        )
        database.execute(
            'INSERT INTO "SP_khungGiaThuPhi" (id, baiDoXeId, tenKhungGia, moTa, trangThai, ngayHieuLuc, '
            'ngayHetHieuLuc, ngayTao, nguoiTao, ghiChu) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)',
            (frame_id, lot_id, f"Khung gia gia lap {lot_number:02d}", "Gia minh hoa, khong phai gia thuc te", "ACTIVE",
             effective_from, effective_until, created_at, CREATOR, note),
        )
        database.execute(
            'INSERT INTO "SP_apDungKhungGia" (id, baiDoXeId, khungGiaId, ngayApDung, trangThaiApDung, ngayTao, nguoiTao, ghiChu) '
            'VALUES (?, ?, ?, ?, ?, ?, ?, ?)',
            (new_id(), lot_id, frame_id, effective_from, "APPLIED", created_at, CREATOR, note),
        )
        for (vehicle_type, rate_day), (initial_price, extra_price) in RATES.items():
            database.execute(
                'INSERT INTO "SP_chiTietMucThuKhungGia" (id, idKhungGia, loaiPhuongTien, loaiNgay, soGioDau, '
                'giaGioDau, giaGioTiepTheo, ngayTao, nguoiTao, ghiChu) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)',
                (new_id(), frame_id, vehicle_type, rate_day, 1,
                 initial_price, extra_price, created_at, CREATOR, note),
            )

        # Independent report fixture: five 1/2/3-hour examples per vehicle type; no visits are persisted.
        revenue = 0
        for vehicle_type in ("MOTORBIKE", "CAR_9_SEATS"):
            first_price, extra_price = RATES[(vehicle_type, day_type)]
            revenue += 5 * sum(first_price + extra_hours * extra_price for extra_hours in range(3))
        database.execute(
            'INSERT INTO "SP_ketQuaKinhDoanhBaiDoXe" (id, baiDoXeId, kyBaoCao, doanhThu, soLuotXe, ngayTao, nguoiTao, ghiChu) '
            'VALUES (?, ?, ?, ?, ?, ?, ?, ?)',
            (new_id(), lot_id, completed_day.strftime("%Y-%m"), revenue, 30, created_at, CREATOR,
             "Independent synthetic monthly result; not aggregated from parking bookings."),
        )


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--database", type=Path, default=DIRECTORY / "parking_business_result.db")
    parser.add_argument("--as-of", help="Reference time with UTC offset, for repeatable testing")
    args = parser.parse_args()
    database_path = args.database.resolve()
    if not database_path.is_file():
        raise SystemExit("Database not found. Apply migrations 001, 002 and 003 first.")
    as_of = datetime.fromisoformat(args.as_of) if args.as_of else datetime.now(LOCAL_ZONE)
    if as_of.utcoffset() is None:
        raise SystemExit("The reference time must include a UTC offset.")
    as_of = as_of.astimezone(LOCAL_ZONE).replace(microsecond=0)
    backup_path = database_path.with_name(f"{database_path.stem}.backup-demo-{uuid.uuid4().hex}.db")

    with closing(sqlite3.connect(database_path.as_uri() + "?mode=rw", uri=True)) as database:
        database.execute("PRAGMA foreign_keys = ON")
        required_tables = {"SP_donViKinhDoanhQuanLy", "DL_KTHT_baiDoXe", "SP_ketQuaKinhDoanhBaiDoXe"}
        existing_tables = {row[0] for row in database.execute("SELECT name FROM sqlite_master WHERE type = 'table'")}
        if not required_tables.issubset(existing_tables):
            raise SystemExit("Required business tables are missing; apply migrations 001, 002 and 003 first.")
        if existing_tables.intersection(NEW_TABLES):
            raise SystemExit("A price table already exists; no changes were applied.")
        if "daXoa" not in {row[1] for row in database.execute('PRAGMA table_info("SP_ketQuaKinhDoanhBaiDoXe")')}:
            raise SystemExit("Soft-delete migration 003 is missing; no changes were applied.")
        if database.execute("PRAGMA foreign_key_check").fetchall():
            raise SystemExit("Database has foreign-key violations; no changes were applied.")

        with backup_path.open("xb"):
            pass
        with closing(sqlite3.connect(backup_path)) as backup:
            database.backup(backup)

        try:
            database.execute("BEGIN IMMEDIATE")
            execute_schema(database, DIRECTORY / "004_parking_price_tables.sql")
            seed(database, as_of)
            if database.execute("PRAGMA foreign_key_check").fetchall():
                raise ValueError("Foreign-key validation failed.")
            database.commit()
        except Exception:
            database.rollback()
            raise

    print(f"Created 5 simulated lots, price references and independent business totals; no visits created. Backup: {backup_path}")


if __name__ == "__main__":
    main()
