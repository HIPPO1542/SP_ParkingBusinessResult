import json
import sqlite3
import subprocess
import sys
import tempfile
import unittest
from contextlib import closing
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
POSTMAN = ROOT / "postman"
SQLITE = ROOT / "database/sqlite"


class PostmanCollectionTests(unittest.TestCase):
    def test_visit_collection_requests_and_scripts(self):
        collection = json.loads((POSTMAN / 'ParkingVisits.postman_collection.json').read_text(encoding='utf-8'))
        self.assertEqual(collection['info']['schema'], 'https://schema.getpostman.com/json/collection/v2.1.0/collection.json')
        requests = collection['item']
        routes = {(item['request']['method'], item['request']['url'].split('?')[0]) for item in requests}
        self.assertEqual(routes, {
            ('POST', '{{baseUrl}}/dev/auth/login'),
            ('POST', '{{baseUrl}}/gdci/parking/parkingLot/businessOperation/import'),
            ('GET', '{{baseUrl}}/gdci/parking/parkingLot/parkingVisit'),
            ('GET', '{{baseUrl}}/gdci/parking/parkingLot/parkingVisit/summary'),
            ('GET', '{{baseUrl}}/gdci/parking/parkingLot/parkingVisit/{{visitId}}/tariff-comparison'),
            ('POST', '{{baseUrl}}/gdci/parking/parkingLot/parkingVisit/export'),
        })
        for item in requests:
            for event in item.get('event', []):
                script = '\n'.join(event['script']['exec'])
                check = subprocess.run(['node', '--check'], input=script, text=True, encoding='utf-8', capture_output=True, check=False)
                self.assertEqual(check.returncode, 0, f"{item['name']}: {check.stderr}")

    def test_collection_requests_and_scripts(self):
        collection = json.loads((POSTMAN / "ParkingBusinessResult.postman_collection.json").read_text(encoding="utf-8"))
        self.assertEqual(collection["info"]["schema"], "https://schema.getpostman.com/json/collection/v2.1.0/collection.json")
        requests = [item for folder in collection["item"] for item in folder["item"]]
        routes = {(item["request"]["method"], item["request"]["url"]) for item in requests}
        self.assertEqual(routes, {
            ("POST", "{{baseUrl}}/dev/auth/login"),
            ("POST", "{{baseUrl}}/dev/auth/users"),
            ("GET", "{{baseUrl}}/gdci/parking/parkingLot/businessOperation"),
            ("POST", "{{baseUrl}}/gdci/parking/parkingLot/businessOperation"),
            ("GET", "{{baseUrl}}/gdci/parking/parkingLot/businessOperation/{{resultId}}"),
            ("PUT", "{{baseUrl}}/gdci/parking/parkingLot/businessOperation/{{resultId}}"),
            ("DELETE", "{{baseUrl}}/gdci/parking/parkingLot/businessOperation/{{resultId}}"),
            ("GET", "{{baseUrl}}/gdci/parking/parkingLot/businessOperation/{{deletedResultId}}"),
        })
        for item in requests:
            for event in item.get("event", []):
                script = "\n".join(event["script"]["exec"])
                check = subprocess.run(["node", "--check"], input=script, text=True, encoding="utf-8", capture_output=True, check=False)
                self.assertEqual(check.returncode, 0, f"{item['name']}: {check.stderr}")

    def test_seed_creates_related_rows_and_rolls_back_on_repeat(self):
        connection = sqlite3.connect(":memory:")
        try:
            for name in ("001_create_business_result.sql", "002_business_reference_tables.sql", "003_soft_delete_business_result.sql"):
                connection.executescript((SQLITE / name).read_text(encoding="utf-8"))
            seed = (POSTMAN / "seed-demo-parking-lot.sql").read_text(encoding="utf-8")
            connection.executescript(seed)
            self.assertEqual(connection.execute('SELECT COUNT(*) FROM "SP_donViKinhDoanhQuanLy"').fetchone(), (1,))
            self.assertEqual(connection.execute('SELECT COUNT(*) FROM "DL_KTHT_baiDoXe"').fetchone(), (1,))
            self.assertEqual(connection.execute("PRAGMA foreign_key_check").fetchall(), [])
            with self.assertRaises(sqlite3.IntegrityError):
                connection.executescript(seed)
            connection.rollback()
            self.assertEqual(connection.execute('SELECT COUNT(*) FROM "DL_KTHT_baiDoXe"').fetchone(), (1,))
        finally:
            connection.close()

    def test_seed_tool_refuses_to_overwrite_existing_rows(self):
        with tempfile.TemporaryDirectory() as temporary_directory:
            root = Path(temporary_directory)
            (root / "postman").mkdir()
            (root / "database/sqlite").mkdir(parents=True)
            for filename in ("seed_demo.py", "seed-demo-parking-lot.sql"):
                (root / "postman" / filename).write_bytes((POSTMAN / filename).read_bytes())
            database_path = root / "database/sqlite/parking_business_result.db"
            with closing(sqlite3.connect(database_path)) as database:
                for name in ("001_create_business_result.sql", "002_business_reference_tables.sql", "003_soft_delete_business_result.sql"):
                    database.executescript((SQLITE / name).read_text(encoding="utf-8"))
            command = [sys.executable, str(root / "postman/seed_demo.py")]
            first_run = subprocess.run(command, capture_output=True, encoding="utf-8", check=False)
            self.assertEqual(first_run.returncode, 0, first_run.stderr)
            repeated_run = subprocess.run(command, capture_output=True, encoding="utf-8", check=False)
            self.assertNotEqual(repeated_run.returncode, 0)
            with closing(sqlite3.connect(database_path)) as database:
                self.assertEqual(database.execute('SELECT COUNT(*) FROM "DL_KTHT_baiDoXe"').fetchone(), (1,))


if __name__ == "__main__":
    unittest.main()
