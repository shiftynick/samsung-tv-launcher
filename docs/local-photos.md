# Local photo screen

Home's Photos shortcut opens an owner-installed album. The first version plays resized copies from the launcher's own app data, so the computer can sleep after installation. It does not start a photo server, scan other computers, expose the original archive, or add voice recording. A future authenticated LAN bridge could synchronize this same bounded album format.

## Remote controls

- Left / Right: previous / next photo, wrapping within the selected album.
- Up / Down: previous / next album; the All Albums choice includes every installed photo.
- Select: pause / resume the slideshow. Back or Home returns to the launcher.

Slides advance every twelve seconds. Crossfades use two reusable native image views; Motion Off switches instantly. Photos retain their aspect ratio with black margins rather than being cropped. The album label and control hint disappear after five seconds during playback and stay visible while paused. Timer/fade work stops when leaving Photos or when another app takes the foreground. Existing OLED care and system screen-saver settings are not changed.

The album is reloaded when Photos opens. Selection position and playback pause are currently session state, not saved across launcher restarts. App/HDMI shortcuts and native settings remain on Home.

## Prepare a private album

Install Pillow on the host. HEIF originals additionally need pillow-heif and a working native HEIF decoder; the optional import is not required for JPEG/PNG sources. Use an isolated environment appropriate to your host.

Create an ignored selection file such as `.local/photo-selection.json`:

```json
{
  "photos": [
    {"source": "example/landscape.jpg", "album": "Example trip", "caption": ""}
  ]
}
```

`source` is relative to an explicitly selected original-photo root. Album/caption values are private display text; do not commit actual selections, filenames, travel details or photos.

```sh
python3 tools/prepare_photos.py --source-root /path/to/originals --selection .local/photo-selection.json --output .local/photo-album
DOTNET_ROLL_FORWARD=Major dotnet run --project tests/PhotoCatalogTests.csproj -- .local/photo-album
```

The exporter refuses an existing output folder and an output inside the original root. It reads originals without writing them, honors orientation, converts tagged color profiles to sRGB, preserves framing, and exports at most 1920×1080. Fresh JPEGs have no inherited EXIF/GPS/XMP, source filenames or comments. The opaque exported filenames and `photos.json` contain only the chosen album/caption text. A failed export can leave a partial private output folder; inspect it and retry in a new folder rather than overwriting source data.

The TV parser accepts schema version 1, 1–200 landscape JPEGs, 4 MiB per image and 100 MiB per album bundle. It validates dimensions, bounded manifest text, known fields, safe local paths, duplicate filenames and symlinks. It does not fetch image URLs or decode downloaded theme scripts. The host validator and native image Ready checks serve different purposes; file-header validation is not full image decoding.

## Owner-authorized installation

Follow [S90D setup](10-s90d-setup.md) and [recovery](09-recovery.md). Keep source/generated albums outside the public package assets and public Git index. Install the validated folder as `photos/` under the application's actual private writable data directory, matching its existing owner and SMACK access label. For the recorded container that directory is `/opt/usr/home/owner/apps_rw/com.samsung.tv.bixby-promotion/data/`; verify the actual path on each TV.

Back up the previous launcher and album on TV and host, prepare inverse operations, and stop the launcher before replacing its assembly or album. The initial installation requires the launcher version that implements Photos. Later album replacements do not need a rebuild: install a complete validated folder and reopen Photos. Do not overwrite the first original backup or restore whole registry databases over live state.

For an existing travel-management project, reuse its saved accepted trip groups and classifications read-only. Do not silently restart paused archive/model jobs, run new paid inference, claim inferred trip membership as verified per-image GPS, or publish catalogs/identity data. A contact-sheet review can remove repeat compositions and weak selections before export.
