import hashlib
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest
try:
    from PIL import Image
except ImportError:
    Image = None
spec = importlib.util.spec_from_file_location('prepare_photos', Path(__file__).resolve().parents[1] / 'tools/prepare_photos.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


@unittest.skipIf(Image is None, 'Optional Pillow required for photo-export tests')
class PhotoExportTests(unittest.TestCase):
    def test_preserves_original_and_strips_metadata(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp) / 'source'; root.mkdir()
            original = root / 'example.jpg'
            exif = Image.Exif(); exif[270] = 'private description'; exif[274] = 3
            Image.new('RGB', (240, 120), 'navy').save(original, exif=exif)
            before = hashlib.sha256(original.read_bytes()).hexdigest()
            selection = Path(temp) / 'selection.json'
            selection.write_text(json.dumps({'photos': [{'source': 'example.jpg', 'album': 'Example'}]}))
            output = Path(temp) / 'output'
            result = module.prepare(root, selection, output)
            self.assertEqual(result['photos'], 1)
            self.assertEqual(hashlib.sha256(original.read_bytes()).hexdigest(), before)
            with Image.open(output / 'images/photo-0001.jpg') as exported:
                self.assertFalse(exported.getexif())
                self.assertNotIn('exif', exported.info)
                self.assertEqual(exported.size, (240, 120))
            self.assertNotIn(str(root), (output / 'photos.json').read_text())
            with self.assertRaises(ValueError): module.prepare(root, selection, output)

    def test_rejects_escape_and_portrait(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp) / 'source'; root.mkdir()
            Image.new('RGB', (100, 200), 'navy').save(root / 'portrait.jpg')
            selection = Path(temp) / 'selection.json'
            for i, name in enumerate(('../outside.jpg', 'portrait.jpg')):
                selection.write_text(json.dumps({'photos': [{'source': name, 'album': 'Example'}]}))
                with self.assertRaises(ValueError): module.prepare(root, selection, Path(temp) / f'output-{i}')


if __name__ == '__main__': unittest.main()
