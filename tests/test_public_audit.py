import importlib.util
from pathlib import Path
import struct
import subprocess
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location("audit_public", ROOT / "tools/audit_public.py")
audit = importlib.util.module_from_spec(spec)
spec.loader.exec_module(audit)


class PublicAuditTests(unittest.TestCase):
    def test_documentation_addresses_and_generic_tv_path_are_allowed(self):
        text = b"192.0.2.50 198.51.100.10 203.0.113.5 127.0.0.1 /home/owner/share/tmp/sdk_tools/ /home/owner:/bin/bash"
        self.assertEqual(audit.inspect_blob("docs/example.md", text), [])

    def test_private_data_and_build_artifacts_are_rejected_without_echoing_values(self):
        address = ".".join(map(str, (192, 168, 50, 9)))
        self.assertIn("non-documentation private network address", audit.inspect_blob("config.txt", address.encode()))
        self.assertIn("personal host path", audit.inspect_blob("notes.md", ("/home/" + "example-user/file").encode()))
        self.assertIn("private/build directory", audit.inspect_blob(".local/recovery.json", b"{}"))
        self.assertIn("private/binary artifact type", audit.inspect_blob("framework/Reference.dll", b"source"))
        self.assertIn("credential-shaped value", audit.inspect_blob("config.txt", ("ghp_" + "a" * 30).encode()))

    def test_embedded_png_text_is_rejected(self):
        payload = b"Comment\x00hidden text"
        png = b"\x89PNG\r\n\x1a\n" + struct.pack(">I", len(payload)) + b"tEXt" + payload + b"\0" * 4
        self.assertIn("embedded image metadata", audit.inspect_blob("art.png", png))

    def test_audit_reads_staged_blob_even_if_worktree_is_replaced(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            subprocess.run(["git", "init", "-q"], cwd=root, check=True)
            path = root / "notes.txt"
            address = ".".join(map(str, (10, 20, 30, 40)))
            path.write_text(address)
            subprocess.run(["git", "add", "notes.txt"], cwd=root, check=True)
            path.write_text("clean working copy")
            _, failures = audit.audit_repository(root)
            self.assertEqual(failures, [("notes.txt", "non-documentation private network address")])
