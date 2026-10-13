"""Offline provisioning checks using the documented project Python 3.12 environment."""
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch

from tools.provision_player_python import pinned_requirements, provision

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "tests/artifacts/phase10a_d2"


class PlayerPythonProvisioningTests(unittest.TestCase):
    def test_exact_requirements_pins(self):
        self.assertEqual(pinned_requirements(ROOT / "requirements-phase2.txt"),
                         {"opencv-python": "5.0.0.93", "numpy": "2.2.6"})

    def test_unpinned_requirement_rejected(self):
        with tempfile.TemporaryDirectory() as temp:
            path = Path(temp) / "requirements.txt"
            path.write_text("opencv-python>=5\n")
            with self.assertRaises(RuntimeError):
                pinned_requirements(path)

    def test_unapproved_package_rejected(self):
        with tempfile.TemporaryDirectory() as temp:
            path = Path(temp) / "requirements.txt"
            path.write_text("unapproved-package==1.0\n")
            with self.assertRaises(RuntimeError):
                pinned_requirements(path)

    def test_wrong_python_rejected(self):
        with patch("tools.provision_player_python.sys.version_info", (3, 11)):
            with self.assertRaises(RuntimeError):
                provision(OUT, ROOT / "requirements-phase2.txt")

    def test_wrong_installed_package_rejected(self):
        class WrongPackage:
            version = "0.0"
        with patch("tools.provision_player_python.importlib.metadata.distribution", return_value=WrongPackage()):
            with self.assertRaises(RuntimeError):
                provision(OUT, ROOT / "requirements-phase2.txt")

    def test_actual_bundle_relocates_and_executes_real_opencv_without_override(self):
        OUT.mkdir(parents=True, exist_ok=True)
        # Keep a real package as inspectable evidence for this phase, not a simulated interpreter.
        output = OUT / "provisioned-player"
        output.mkdir(exist_ok=True)
        for package in ("agent", "vision", "camera"):
            for source in (ROOT / package).rglob("*.py"):
                target = output / package / source.relative_to(ROOT / package)
                target.parent.mkdir(parents=True, exist_ok=True)
                shutil.copy2(source, target)
        bundle = provision(output, ROOT / "requirements-phase2.txt")
        manifest = json.loads((bundle / "patrolai-runtime.json").read_text())
        self.assertEqual(manifest["packages"], pinned_requirements(ROOT / "requirements-phase2.txt"))
        self.assertNotIn(str(ROOT), (bundle / "patrolai-runtime.json").read_text())
        self.assertNotIn(str(ROOT), (bundle / "python312._pth").read_text())
        moved = OUT / "relocated-player"
        if moved.exists():
            # Both exact resolved paths are phase-owned, beneath OUT, and never a source clone.
            self.assertEqual(moved.resolve().parent, OUT.resolve())
            shutil.rmtree(moved)
        output.rename(moved)
        environment = {k: os.environ[k] for k in ("SystemRoot", "WINDIR", "TEMP", "TMP") if k in os.environ}
        code = ("import sys,cv2,numpy as np,agent,vision,camera,importlib.metadata as m; "
                "assert m.version('opencv-python')=='5.0.0.93'; assert np.__version__=='2.2.6'; "
                "pixels=np.zeros((12,12,3),dtype=np.uint8); pixels[2:8,3:9]=(0,0,255); "
                "assert cv2.imwrite('opencv-smoke.png',pixels); "
                "assert np.array_equal(cv2.imread('opencv-smoke.png'),pixels); "
                "assert not any('.venv-phase2' in p for p in sys.path); "
                "print('REAL_LOCAL_PYTHON_OPENCV=PASS; MODEL_INFERENCE=NOT_RUN')")
        result = subprocess.run([str(moved / ".patrolai-python/python.exe"), "-c", code],
                                cwd=moved, env=environment, capture_output=True, text=True, timeout=60)
        self.assertEqual(result.returncode, 0, result.stderr)
        (OUT / "real-python-opencv.log").write_text(result.stdout + result.stderr, encoding="utf-8")
        self.assertTrue((moved / "opencv-smoke.png").is_file())


if __name__ == "__main__":
    unittest.main()
