#!/usr/bin/env python3
"""Export a reviewed private selection to a bounded, metadata-free TV photo album."""
import argparse
import json
import os
from pathlib import Path


def prepare(root, selection, output):
    from PIL import Image, ImageCms, ImageOps
    try:
        import pillow_heif
        pillow_heif.register_heif_opener()
    except ImportError:
        pass  # JPEG/PNG work without the optional HEIF decoder.
    root, output = Path(root).resolve(), Path(output).resolve()
    if not root.is_dir() or output.exists() or output.is_relative_to(root):
        raise ValueError('Use an existing source root and a new output directory outside it.')
    document = json.loads(Path(selection).read_text())
    rows = document.get('photos', [])
    if not 1 <= len(rows) <= 200:
        raise ValueError('Select between 1 and 200 photos.')
    planned = []
    for row in rows:
        relative = Path(row['source'])
        source = (root / relative).resolve()
        if relative.is_absolute() or '..' in relative.parts or not source.is_relative_to(root) or not source.is_file():
            raise ValueError('A selected source is missing or escapes its root.')
        album, caption = row.get('album', 'Photos'), row.get('caption', '')
        if not isinstance(album, str) or not album.strip() or len(album) > 100 or not isinstance(caption, str) or len(caption) > 160 or any(ord(c) < 32 for c in album + caption):
            raise ValueError('Invalid album or caption.')
        if source.stat().st_size > 200 * 1024 * 1024:
            raise ValueError('Source file exceeds the export bound.')
        planned.append((source, album, caption))
    output.mkdir(parents=True, mode=0o700)
    (output / 'images').mkdir(mode=0o700)
    result = []
    total = 0
    for index, (source, album, caption) in enumerate(planned, 1):
        with Image.open(source) as original:
            if original.width * original.height > 100_000_000:
                raise ValueError('Source dimensions exceed the export bound.')
            image = ImageOps.exif_transpose(original)
            profile = image.info.get('icc_profile')
            if profile:
                import io
                image = ImageCms.profileToProfile(image, ImageCms.ImageCmsProfile(io.BytesIO(profile)), ImageCms.createProfile('sRGB'), outputMode='RGB')
            else:
                image = image.convert('RGB')
            image.thumbnail((1920, 1080), Image.Resampling.LANCZOS)
            if image.width <= image.height:
                raise ValueError('Select landscape-oriented photos for this album.')
            # A fresh image has no inherited EXIF/GPS, comments, XMP or source filename.
            clean = Image.new('RGB', image.size)
            clean.paste(image)
            name = f'images/photo-{index:04d}.jpg'
            target = output / name
            clean.save(target, format='JPEG', quality=82, optimize=True)
            os.chmod(target, 0o600)
            total += target.stat().st_size
            if total > 100 * 1024 * 1024:
                raise ValueError('Album exceeds the 100 MiB bound; export fewer images.')
            result.append({'file': name, 'album': album, 'caption': caption})
    manifest = output / 'photos.json'
    manifest.write_text(json.dumps({'schemaVersion': 1, 'photos': result}, indent=2) + '\n')
    os.chmod(manifest, 0o600)
    return {'photos': len(result), 'albums': len(set(row['album'] for row in result)), 'bytes': total}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--source-root', required=True, type=Path)
    parser.add_argument('--selection', required=True, type=Path)
    parser.add_argument('--output', required=True, type=Path)
    args = parser.parse_args()
    print(json.dumps(prepare(args.source_root, args.selection, args.output)))


if __name__ == '__main__':
    main()
