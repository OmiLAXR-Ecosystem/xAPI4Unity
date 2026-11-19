#!/usr/bin/env python3
"""Checks the Unity package rules that CI can verify without Unity.

- Every file and folder Unity imports has a committed .meta, and every .meta
  belongs to a committed file or folder.
- Every .meta has a unique 32-hex guid.
- Every .cs file starts with an SPDX license header.
- package.json is valid, and its version matches CITATION.cff and, if the
  repo has one, the release-please manifest.

Run from anywhere: python3 .github/scripts/check_package.py
Errors are printed as GitHub annotations.
"""

import json
import re
import subprocess
import sys
from pathlib import Path, PurePosixPath

ROOT = Path(__file__).resolve().parents[2]
SEMVER = re.compile(r"^\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?(\+[0-9A-Za-z.-]+)?$")
GUID = re.compile(r"^guid:\s*([0-9a-f]{32})\s*$", re.MULTILINE)

errors = []


def error(message, file=None):
    errors.append((file, message))


def tracked_files():
    out = subprocess.run(
        ["git", "ls-files", "-z"], cwd=ROOT, check=True, capture_output=True
    ).stdout.decode("utf-8")
    return [PurePosixPath(p) for p in out.split("\0") if p]


def ignored_by_unity(path):
    # Unity skips hidden entries, folders ending in "~", "cvs" and *.tmp.
    for part in path.parts:
        if part.startswith(".") or part.endswith("~") or part.lower() == "cvs":
            return True
    return path.name.endswith(".tmp")


def check_meta_files(files):
    assets = set()
    metas = set()
    for path in files:
        if ignored_by_unity(path):
            continue
        if path.suffix == ".meta":
            metas.add(path)
        else:
            assets.add(path)
            assets.update(p for p in path.parents if p != PurePosixPath("."))

    for asset in sorted(assets):
        if PurePosixPath(f"{asset}.meta") not in metas:
            error(f"Missing {asset.name}.meta. Let Unity create it, or copy a neighbor's .meta with a new guid.", str(asset))

    guids = {}
    for meta in sorted(metas):
        target = PurePosixPath(str(meta)[: -len(".meta")])
        if target not in assets:
            error("Orphaned .meta: its file or folder does not exist.", str(meta))
        match = GUID.search((ROOT / meta).read_text(encoding="utf-8-sig"))
        if not match:
            error("No valid 32-hex guid.", str(meta))
            continue
        guid = match.group(1)
        if guid in guids:
            error(f"Duplicate guid {guid}, also used by {guids[guid]}.", str(meta))
        else:
            guids[guid] = str(meta)


def check_spdx_headers(files):
    for path in files:
        if path.suffix != ".cs" or ignored_by_unity(path):
            continue
        head = (ROOT / path).read_text(encoding="utf-8-sig").splitlines()[:10]
        if not any("SPDX-License-Identifier:" in line for line in head):
            error("Missing SPDX license header. Copy it from a neighboring file.", str(path))


def check_versions():
    try:
        package = json.loads((ROOT / "package.json").read_text(encoding="utf-8"))
    except (OSError, ValueError) as e:
        error(f"Cannot read package.json: {e}", "package.json")
        return

    for field in ("name", "version", "displayName", "unity"):
        if not package.get(field):
            error(f'package.json has no "{field}".', "package.json")
    version = package.get("version", "")
    if version and not SEMVER.match(version):
        error(f'"{version}" is not a SemVer version.', "package.json")

    manifest_path = ".github/.release-please-manifest.json"
    if (ROOT / manifest_path).exists():
        try:
            manifest = json.loads((ROOT / manifest_path).read_text(encoding="utf-8"))
            if manifest.get(".") != version:
                error(f'Version {manifest.get(".")} differs from package.json ({version}).', manifest_path)
        except ValueError as e:
            error(f"Cannot read the release-please manifest: {e}", manifest_path)

    citation = ROOT / "CITATION.cff"
    if citation.exists():
        found = re.findall(r"^\s*version:\s*([^\s#]+)", citation.read_text(encoding="utf-8"), re.MULTILINE)
        for cff_version in found:
            if cff_version.strip("\"'") != version:
                error(f"Version {cff_version} differs from package.json ({version}).", "CITATION.cff")


def main():
    files = tracked_files()
    check_meta_files(files)
    check_spdx_headers(files)
    check_versions()

    for file, message in errors:
        location = f" file={file}" if file else ""
        print(f"::error{location}::{message}")
    if errors:
        print(f"{len(errors)} problem(s) found.")
        return 1
    print("Package rules OK.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
