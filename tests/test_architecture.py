"""Prevent cross-layer dependencies from creeping back into the module."""
import unittest
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
BACKEND = ROOT / "Backend"
PREFIX = "ParkingBusinessResult."


class ArchitectureChecks(unittest.TestCase):
    def test_project_dependency_direction(self):
        allowed = {
            "Domain": set(),
            "Application": {"Domain"},
            "Infrastructure": {"Application"},
            "Api": {"Application", "Infrastructure"},
        }
        for layer, dependencies in allowed.items():
            project = BACKEND / (PREFIX + layer) / (PREFIX + layer + ".csproj")
            references = ET.parse(project).findall(".//ProjectReference")
            actual = {Path(reference.attrib["Include"].replace("\\", "/")).stem.removeprefix(PREFIX) for reference in references}
            self.assertEqual(dependencies, actual, layer)

    def test_endpoints_do_not_call_infrastructure(self):
        for source in (BACKEND / (PREFIX + "Api") / "Endpoint").rglob("*.cs"):
            self.assertNotIn("ParkingBusinessResult.Infrastructure", source.read_text(encoding="utf-8-sig"), str(source))

    def test_inner_layers_have_no_technology_packages(self):
        for layer in ("Domain", "Application"):
            project = BACKEND / (PREFIX + layer) / (PREFIX + layer + ".csproj")
            self.assertEqual([], ET.parse(project).findall(".//PackageReference"), layer)
            for source in project.parent.rglob("*.cs"):
                if "obj" in source.parts or "bin" in source.parts:
                    continue
                content = source.read_text(encoding="utf-8-sig")
                for dependency in ("Microsoft.Data.Sqlite", "Microsoft.AspNetCore", "ExcelDataReader", "PdfSharp"):
                    self.assertNotIn(dependency, content, str(source))


if __name__ == "__main__":
    unittest.main()
