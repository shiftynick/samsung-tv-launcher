#!/usr/bin/env python3
"""Audit indexed Git blobs, not ignored files or unstaged working-tree replacements."""
import ipaddress
from pathlib import Path, PurePosixPath
import re
import struct
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
DOCUMENTATION_NETS = tuple(ipaddress.ip_network(s) for s in (
    "192.0.2.0/24", "198.51.100.0/24", "203.0.113.0/24"))
FORBIDDEN_SUFFIXES = {".dll", ".exe", ".so", ".pdb", ".tpk", ".wgt", ".db", ".sqlite", ".sqlite3",
                      ".pem", ".key", ".token", ".log", ".pcap", ".pcapng", ".har", ".zip", ".gz", ".tar"}
TOKEN = re.compile(r"\b(?:gh[pousr]_[A-Za-z0-9]{20,}|github_pat_[A-Za-z0-9_]{20,}|sk-[A-Za-z0-9_-]{20,})")
HOST_PATH = re.compile(r"/(?:home|Users)/[^/\s\"'<>]+/")
IPV4 = re.compile(r"(?<![\w.])(?:\d{1,3}\.){3}\d{1,3}(?![\w.])")
MAC = re.compile(r"\b(?:[0-9a-fA-F]{2}:){5}[0-9a-fA-F]{2}\b")
PRIVATE_KEY = re.compile(r"-----BEGIN (?:[A-Z]+ )?PRIVATE KEY-----")


def inspect_blob(name, data):
    problems = []
    path = PurePosixPath(name)
    if any(p in {".local", ".venv", "__pycache__", "node_modules", "bin", "obj"} for p in path.parts):
        problems.append("private/build directory")
    if path.suffix.lower() in FORBIDDEN_SUFFIXES or path.name.startswith(".env") and path.name != ".env.example":
        problems.append("private/binary artifact type")
    if path.name in {"apps.local.json", "config.toml", "credentials.json"}:
        problems.append("local configuration")
    if path.suffix.lower() == ".png":
        if not data.startswith(b"\x89PNG\r\n\x1a\n"):
            return problems + ["invalid PNG"]
        offset = 8
        while offset + 12 <= len(data):
            size = struct.unpack_from(">I", data, offset)[0]
            kind = data[offset + 4:offset + 8]
            if offset + 12 + size > len(data):
                problems.append("truncated PNG")
                break
            if kind in {b"tEXt", b"zTXt", b"iTXt", b"eXIf", b"caBX"}:
                problems.append("embedded image metadata")
            offset += size + 12
            if kind == b"IEND":
                break
        return sorted(set(problems))
    if path.suffix.lower() in {".ttf", ".otf", ".jpg", ".jpeg"}:
        # This project publishes PNG previews only; fonts have separate license files.
        if path.suffix.lower() != ".ttf":
            problems.append("unreviewed binary image/font type")
        return problems
    try:
        text = data.decode("utf-8")
    except UnicodeDecodeError:
        return problems + ["unreviewed binary file"]
    for label, pattern in (("credential-shaped value", TOKEN), ("private-key material", PRIVATE_KEY), ("MAC-shaped identifier", MAC)):
        if pattern.search(text):
            problems.append(label)
    for match in HOST_PATH.finditer(text):
        # /home/owner is the generic path supplied by Samsung's TV SDK, not a host username.
        if match.group() not in {"/home/owner/", "/home/owner:/"}:
            problems.append("personal host path")
    for match in IPV4.finditer(text):
        try:
            address = ipaddress.ip_address(match.group())
        except ValueError:
            continue
        if address.is_loopback or address.is_unspecified or any(address in net for net in DOCUMENTATION_NETS):
            continue
        if address.is_private or address.is_link_local:
            problems.append("non-documentation private network address")
    return sorted(set(problems))


def audit_repository(root=ROOT):
    names = subprocess.check_output(["git", "ls-files", "--cached", "-z"], cwd=root).decode().split("\0")
    names = [name for name in names if name]
    if not names:
        raise ValueError("No indexed publish set. Stage reviewed source/assets first.")
    failures = []
    for name in names:
        data = subprocess.check_output(["git", "show", ":" + name], cwd=root)
        for problem in inspect_blob(name, data):
            failures.append((name, problem))
    return names, failures


def main():
    try:
        names, failures = audit_repository()
    except (ValueError, subprocess.CalledProcessError) as error:
        print("Audit stopped:", error, file=sys.stderr)
        return 2
    if failures:
        for name, problem in failures:
            # Never echo a matched secret, address or identifier.
            print(f"{name}: {problem}", file=sys.stderr)
        return 1
    print(f"Public index audit passed: {len(names)} source/artwork/license files.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
