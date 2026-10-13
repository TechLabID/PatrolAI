"""Provision an isolated, relocatable Windows Python runtime from the validated project env.
No network, venv copy, credentials, PATH search, or machine-specific runtime configuration.
"""
import argparse
import hashlib
import importlib.metadata
import json
import os
from pathlib import Path
import re
import shutil
import struct
import subprocess
import sys
import tempfile
import uuid
import zipfile

BUNDLE_NAME = ".patrolai-python"
MANIFEST = "patrolai-runtime.json"


def pinned_requirements(path):
    pins = {}
    for line in path.read_text(encoding="utf-8").splitlines():
        line = line.strip()
        if not line or line.startswith("#"):
            continue
        match = re.fullmatch(r"([a-zA-Z0-9_-]+)==([0-9.]+)", line)
        if not match:
            raise RuntimeError("Runtime provisioning requires exact package pins")
        pins[match[1]] = match[2]
    if set(pins) != {"opencv-python", "numpy"}:
        raise RuntimeError("Runtime allowlist must match requirements-phase2.txt")
    return pins


def provision(output_root, requirements):
    if os.name != "nt" or sys.version_info[:2] != (3, 12) or struct.calcsize("P") != 8:
        raise RuntimeError("Provisioning requires Windows Python 3.12 x64")
    pins = pinned_requirements(requirements)
    distributions = []
    for name, version in pins.items():
        dist = importlib.metadata.distribution(name)
        if dist.version != version:
            raise RuntimeError("Installed package does not match pin: " + name)
        distributions.append(dist)
    output_root = output_root.resolve(strict=True)
    destination = output_root / BUNDLE_NAME
    base = Path(sys.base_prefix).resolve(strict=True)
    if output_root == base or output_root in Path(sys.executable).resolve().parents:
        raise RuntimeError("Output must be separate from the provisioning interpreter")
    if destination.is_symlink():
        raise RuntimeError("Refusing a linked runtime destination")
    if destination.exists():
        manifest = destination / MANIFEST
        if not manifest.is_file() or json.loads(manifest.read_text())["schemaVersion"] != 1:
            raise RuntimeError("Refusing to replace an unrecognized runtime directory")
    with tempfile.TemporaryDirectory(prefix="patrolai-runtime-", dir=output_root) as temp:
        stage = Path(temp)
        for name in ("python.exe", "python312.dll", "python3.dll", "LICENSE.txt"):
            shutil.copy2(base / name, stage / name)
        for name in ("vcruntime140.dll", "vcruntime140_1.dll"):
            if (base / name).is_file():
                shutil.copy2(base / name, stage / name)
        (stage / "DLLs").mkdir()
        for source in sorted((base / "DLLs").iterdir()):
            if source.suffix.lower() in (".pyd", ".dll") and source.stem != "_tkinter":
                shutil.copy2(source, stage / "DLLs" / source.name)
        # No local site-packages, bytecode paths, pip configuration, tests, or user data.
        excluded = {"site-packages", "test", "tests", "idlelib", "tkinter", "ensurepip", "__pycache__"}
        with zipfile.ZipFile(stage / "python312.zip", "w", zipfile.ZIP_DEFLATED) as archive:
            for source in sorted((base / "Lib").rglob("*.py")):
                relative = source.relative_to(base / "Lib")
                if not excluded.intersection(relative.parts):
                    info = zipfile.ZipInfo(relative.as_posix(), (1980, 1, 1, 0, 0, 0))
                    info.compress_type = zipfile.ZIP_DEFLATED
                    archive.writestr(info, source.read_bytes())
        site = stage / "Lib" / "site-packages"
        site.mkdir(parents=True)
        for dist in distributions:
            site_root = Path(dist.locate_file("")).resolve(strict=True)
            if not dist.files:
                raise RuntimeError("Package lacks an auditable file manifest")
            for relative in dist.files:
                if ".." in relative.parts or relative.is_absolute():
                    continue  # CLI launchers outside site-packages are not runtime assets.
                if "__pycache__" in relative.parts or relative.suffix == ".pyc" or relative.name in ("direct_url.json", "REQUESTED", "INSTALLER", "RECORD"):
                    continue
                source = Path(dist.locate_file(relative)).resolve(strict=True)
                if not source.is_relative_to(site_root):
                    raise RuntimeError("Package file escapes the audited site-packages root")
                target = site / relative
                target.parent.mkdir(parents=True, exist_ok=True)
                shutil.copy2(source, target)
        # _pth isolates registry/PYTHONPATH discovery; all entries remain bundle-relative.
        (stage / "python312._pth").write_text("python312.zip\n.\nDLLs\nLib/site-packages\n..\nimport site\n", encoding="utf-8")
        manifest = {"schemaVersion": 1, "pythonVersion": ".".join(map(str, sys.version_info[:3])),
                    "architecture": "x64", "packages": pins,
                    "requirementsSha256": hashlib.sha256(requirements.read_bytes()).hexdigest()}
        (stage / MANIFEST).write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
        validate_bundle(stage, pins)
        backup = output_root / (BUNDLE_NAME + "-previous-" + uuid.uuid4().hex)
        if destination.exists():
            destination.rename(backup)
        try:
            stage.rename(destination)
        except Exception:
            if backup.exists():
                backup.rename(destination)
            raise
        if backup.exists():
            # This is the checked, owned destination moved above, never an arbitrary path.
            shutil.rmtree(backup)
    validate_bundle(destination, pins)
    print("PATROLAI_PYTHON_BUNDLE_READY python=3.12 architecture=x64 packages=pinned")
    return destination


def validate_bundle(bundle, pins):
    check = ("import sys,struct,cv2,numpy,agent,vision,camera,importlib.metadata as m; "
             "assert sys.version_info[:2]==(3,12) and struct.calcsize('P')==8; "
             "assert m.version('opencv-python')==" + repr(pins['opencv-python']) + "; "
             "assert m.version('numpy')==" + repr(pins['numpy']) + "; "
             "assert not any('.venv-phase2' in p for p in sys.path)")
    # Validation does not inherit any inference credentials or Python overrides.
    environment = {k: os.environ[k] for k in ("SystemRoot", "WINDIR", "TEMP", "TMP") if k in os.environ}
    result = subprocess.run([str(bundle / "python.exe"), "-c", check],
                            cwd=bundle.parent, env=environment, capture_output=True, timeout=60)
    if result.returncode:
        raise RuntimeError("Provisioned runtime import/version validation failed (exit " + str(result.returncode) + ")")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output-root", type=Path, required=True)
    parser.add_argument("--requirements", type=Path, default=Path(__file__).resolve().parents[1] / "requirements-phase2.txt")
    args = parser.parse_args()
    try:
        provision(args.output_root, args.requirements.resolve(strict=True))
    except Exception as error:
        # Only public setup failures; no environment dumps or child output.
        print("PATROLAI_PYTHON_BUNDLE_FAILED type=" + type(error).__name__ + " reason=" + str(error), file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
