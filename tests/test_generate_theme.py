import io
import json
import sys
import tempfile
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "tools"))

try:
    from PIL import Image
    import generate_theme as gt
except ImportError:  # Pillow is optional on hosts that do not generate themes.
    gt = None


@unittest.skipIf(gt is None, "Pillow is not installed")
class GenerateThemeTests(unittest.TestCase):
    def test_palette_uses_dark_base_and_vivid_accent(self):
        image = Image.new("RGB", (160, 90), (12, 20, 40))
        image.paste((240, 120, 30), (0, 0, 40, 20))
        colors = gt.palette(image)
        self.assertLess(gt.luminance(tuple(int(colors["background"][i:i + 2], 16) for i in (1, 3, 5))), 0.2)
        accent = tuple(int(colors["accent"][i:i + 2], 16) for i in (1, 3, 5))
        self.assertGreater(accent[0], accent[2])
        self.assertTrue(all(len(v) in (7, 9) and v.startswith("#") for v in colors.values()))

    def test_fit_crops_to_exact_size(self):
        self.assertEqual(gt.fit(Image.new("RGB", (2560, 1440)), 1920, 1080).size, (1920, 1080))
        self.assertEqual(gt.fit(Image.new("RGB", (1024, 1536)), 512, 512).size, (512, 512))

    def test_prompts_forbid_text_and_logos(self):
        for prompt in [gt.background_prompt("x")] + [gt.tile_prompt("x", app) for app in gt.APPS]:
            self.assertIn("No text", prompt)
            self.assertIn("logos", prompt)

    def test_offline_build_is_a_valid_pack(self):
        with tempfile.TemporaryDirectory() as tmp:
            out = Path(tmp) / "pack"
            gt.main(["misty pines", "--id", "misty-pines", "--name", "Misty Pines", "--out", str(out), "--offline"])
            data = json.loads((out / "theme.json").read_text())
            self.assertEqual(set(data["icons"]), set(gt.APPS))
            with Image.open(out / "background.png") as bg:
                self.assertEqual(bg.size, (1920, 1080))
                self.assertFalse(bg.info.get("exif"))
            with self.assertRaises(SystemExit):
                gt.main(["again", "--id", "misty-pines", "--name", "Misty Pines", "--out", str(out), "--offline"])

    def test_bad_ids_are_refused(self):
        with self.assertRaises(SystemExit):
            gt.main(["x", "--id", "Bad ID", "--name", "x", "--offline"])


if __name__ == "__main__":
    unittest.main()
