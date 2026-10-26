#!/usr/bin/env python3
"""Validate and package the shared RimWorld 1.6/1.7 content and committed DLL."""

import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess
import xml.etree.ElementTree as ET
import zipfile

ROOT = Path(__file__).resolve().parents[1]


def package(root, output, build_id):
    if not re.fullmatch(r"[A-Za-z0-9][A-Za-z0-9._-]*", build_id):
        raise ValueError("Build ID must contain only letters, digits, dots, underscores, or hyphens")

    versions = [v.text for v in ET.parse(root / "About/About.xml").findall("supportedVersions/li")]
    if versions != ["1.6", "1.7"]:
        raise ValueError("Expected support for RimWorld 1.6 and 1.7")
    mappings = ET.parse(root / "LoadFolders.xml").getroot()
    expected = [
        ("Common", {}),
        ("Common/Mods/Animal Armor Framework", {
            "IfModActive": "ingendum.animalapparelframework", "IfModNotActive": "Dylan.AnimalGear"}),
        ("Common/Mods/Animal Gear", {
            "IfModActive": "Dylan.AnimalGear", "IfModNotActive": "ingendum.animalapparelframework"}),
        ("Common/Mods/VEF", {
            "IfModActive": "oskarpotocki.vanillafactionsexpanded.core_steam,oskarpotocki.vanillafactionsexpanded.core"}),
    ]
    if [node.tag for node in mappings] != ["v1.6", "v1.7"]:
        raise ValueError("Expected explicit load mappings for 1.6 and 1.7")
    for node in mappings:
        if [(entry.text, entry.attrib) for entry in node] != expected:
            raise ValueError(f"Unexpected shared content paths or integration conditions in {node.tag}")
        for entry in node:
            if not (root / entry.text).is_dir():
                raise ValueError(f"Missing load folder: {entry.text}")

    assembly = root / "Common/Mods/VEF/Assemblies/Demigryphs.VEFBannerMenuPatch.dll"
    if not assembly.is_file() or assembly.stat().st_size == 0:
        raise ValueError("Missing compiled VEF banner menu patch")
    files = [root / name for name in ("LoadFolders.xml", "README.md", "LICENSE")]
    for folder in ("About", "Common"):
        files.extend(p for p in (root / folder).rglob("*")
                     if p.is_file() and not {"bin", "obj", "__pycache__"}.intersection(p.relative_to(root).parts))
    for path in files:
        if not path.is_file():
            raise ValueError(f"Missing package file: {path}")
        if path.suffix == ".xml":
            ET.parse(path)

    commit = subprocess.check_output(["git", "-C", str(root), "rev-parse", "HEAD"], text=True).strip()
    output.mkdir(parents=True, exist_ok=True)
    archive = output / f"Demigryphs-dev-{build_id}.g{commit[:7]}.zip"
    with zipfile.ZipFile(archive, "w", zipfile.ZIP_DEFLATED) as bundle:
        for path in sorted(files):
            bundle.write(path, "Demigryphs/" + path.relative_to(root).as_posix())
    checksum = hashlib.sha256(archive.read_bytes()).hexdigest()
    (output / "SHA256SUMS").write_text(f"{checksum}  {archive.name}\n", encoding="utf-8")
    identity = {
        "commit": commit,
        "ref": os.environ.get("GITHUB_REF"),
        "event": os.environ.get("GITHUB_EVENT_NAME", "local"),
        "supportedVersions": versions,
        "package": archive.name,
        "packageSha256": checksum,
        "assemblySha256": hashlib.sha256(assembly.read_bytes()).hexdigest(),
        "assemblySource": "Committed DLL; not recompiled by this packaging workflow",
        "fileCount": len(files),
    }
    if os.environ.get("GITHUB_RUN_ID"):
        identity["runUrl"] = (f"{os.environ['GITHUB_SERVER_URL']}/{os.environ['GITHUB_REPOSITORY']}"
                              f"/actions/runs/{os.environ['GITHUB_RUN_ID']}")
    internal = root / ".rimworld-ci"
    internal.mkdir(exist_ok=True)
    (output / "BUILD_INFO.json").unlink(missing_ok=True)
    (internal / "build-info.json").write_text(json.dumps(identity, indent=2) + "\n", encoding="utf-8")
    print(f"Packaged {len(files)} files: {archive}\nSHA-256: {checksum}")
    return archive


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--build-id", default="local")
    parser.add_argument("--output", type=Path, default=ROOT / "dist")
    args = parser.parse_args()
    package(ROOT, args.output, args.build_id)


if __name__ == "__main__":
    main()
