#!/usr/bin/env python3
"""Generate a complete theme pack from one prompt with OpenAI's image API.

Makes a 1920x1080 background and six matching 512x512 app tiles, derives the
palette from the background, writes theme.json and validates the pack with
the same ThemeTool parser the TV uses. App names stay native launcher text;
the art never contains logos or lettering.

    python3 tools/generate_theme.py "rainy Kyoto street at night, lo-fi anime" \
        --id rainy-kyoto --name "Rainy Kyoto"

The API key is read from a private file (default
~/.config/tv-theme-generator/openai-key) and is only sent to OpenAI.
Requires Pillow.
"""
import argparse
import base64
import colorsys
import io
import json
import os
import re
import subprocess
import sys
import time
import urllib.error
import urllib.request
import uuid
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
API = "https://api.openai.com/v1/images/"
DEFAULT_KEY = Path.home() / ".config/tv-theme-generator/openai-key"

# Each tile gets a different subject and the app's familiar colour, so tiles
# stay recognisable without copying any brand mark.
APPS = {
    "plex": ("a guide or explorer figure from this world", "warm amber orange"),
    "netflix": ("a striking creature or mascot from this world", "deep crimson red"),
    "apple-tv": ("an elegant landmark, machine or artefact from this world", "silver and graphite"),
    "prime-video": ("a vehicle or vessel from this world", "bright sky blue"),
    "youtube": ("a lively performer or animal from this world", "vivid red"),
    "youtube-tv": ("a broadcast tower, beacon or lookout from this world", "red with white highlights"),
}
NO_TEXT = "No text, letters, numbers, logos, brand marks, watermarks, user-interface elements, frames or borders."


def background_prompt(style):
    return (
        f"Widescreen 16:9 TV home-screen wallpaper. Theme: {style}. "
        "Composition: an atmospheric establishing scene whose main interest sits in the upper 45% of the frame. "
        "The lower 55% is a calm, darker, low-detail foreground such as ground, water, a floor or haze, "
        "because a row of app tiles and small text will sit on top of it. Cinematic lighting, rich but uncluttered. "
        + NO_TEXT
    )


def tile_prompt(style, app):
    subject, colour = APPS[app]
    return (
        "Square app-tile illustration for a TV launcher, in exactly the same art style, palette and lighting "
        f"as the reference wallpaper. Theme: {style}. Subject: {subject}, centred and filling most of the tile, "
        f"on a simple matching background from the same world. The subject's dominant accent colour is {colour}. "
        + NO_TEXT
    )


# ---- Images and palette -------------------------------------------------

def fit(image, width, height):
    """Scale to cover and centre-crop to an exact size."""
    image = image.convert("RGB")
    scale = max(width / image.width, height / image.height)
    resized = image.resize((max(width, round(image.width * scale)), max(height, round(image.height * scale))), Image.LANCZOS)
    left, top = (resized.width - width) // 2, (resized.height - height) // 2
    return resized.crop((left, top, left + width, top + height))


def save_png(image, path):
    # A fresh save drops any embedded metadata from the source image.
    clean = Image.new("RGB", image.size)
    clean.paste(image.convert("RGB"))
    clean.save(path, "PNG", optimize=True)


def hexcolor(rgb, alpha=None):
    text = "#%02X%02X%02X" % tuple(max(0, min(255, round(c))) for c in rgb)
    return text if alpha is None else text + "%02X" % alpha


def luminance(rgb):
    return (0.2126 * rgb[0] + 0.7152 * rgb[1] + 0.0722 * rgb[2]) / 255


def scale_hsv(rgb, saturation=None, value=None):
    h, s, v = colorsys.rgb_to_hsv(*(c / 255 for c in rgb))
    s = s if saturation is None else saturation(s)
    v = v if value is None else value(v)
    return tuple(c * 255 for c in colorsys.hsv_to_rgb(h, max(0, min(1, s)), max(0, min(1, v))))


def palette(background):
    """Theme palette from the wallpaper: a dark base for panels and a vivid accent."""
    small = background.convert("RGB").resize((160, 90))
    quantized = small.quantize(colors=12, method=Image.Quantize.MEDIANCUT)
    raw = quantized.getpalette()
    counts = sorted(quantized.getcolors(), reverse=True)
    colors = [(count, tuple(raw[i * 3:i * 3 + 3])) for count, i in counts]
    darks = [c for c in colors if luminance(c[1]) < 0.25] or [min(colors, key=lambda c: luminance(c[1]))]
    base = scale_hsv(darks[0][1], value=lambda v: min(v, 0.16))

    def vividness(entry):
        count, rgb = entry
        _, s, v = colorsys.rgb_to_hsv(*(c / 255 for c in rgb))
        return s * v * (count ** 0.3)

    bright = [c for c in colors if luminance(c[1]) >= 0.3] or colors
    accent = scale_hsv(max(bright, key=vividness)[1], saturation=lambda s: max(s, 0.55), value=lambda v: max(v, 0.92))
    card = scale_hsv(base, value=lambda v: v * 0.6)
    muted = tuple(0.85 * a + 0.15 * b for a, b in zip((200, 205, 204), accent))
    return {
        "background": hexcolor(base),
        "text": "#FAF5E6",
        "muted": hexcolor(muted),
        "accent": hexcolor(accent),
        "card": hexcolor(card),
        "control": hexcolor(card, 0xB8),
        "caption": hexcolor(card, 0xE0),
        "overlay": hexcolor(scale_hsv(base, value=lambda v: v * 0.7), 0xDB),
        "header": "#00000033",
        "footer": "#000000A3",
    }


def manifest(theme_id, name, colors):
    return {
        "schemaVersion": 1,
        "id": theme_id,
        "name": name,
        "order": 100,
        "background": "background.png",
        "icons": {app: f"{app}.png" for app in APPS},
        "palette": colors,
        "cornerRadius": 18,
        "pixelArt": False,
        "animationMs": 160,
        "fontFamily": "Quiet Inter",
        "fontVariants": True,
        "license": "LICENSE.txt",
    }


# ---- OpenAI image API -----------------------------------------------------

def request(endpoint, key, body, content_type):
    for attempt in range(4):
        req = urllib.request.Request(API + endpoint, data=body, method="POST",
                                     headers={"Authorization": "Bearer " + key, "Content-Type": content_type})
        try:
            with urllib.request.urlopen(req, timeout=300) as response:
                data = json.load(response)
            return base64.b64decode(data["data"][0]["b64_json"])
        except urllib.error.HTTPError as error:
            detail = error.read().decode("utf-8", "replace")
            if error.code in (429, 500, 502, 503) and attempt < 3:
                time.sleep(5 * (attempt + 1))
                continue
            try:
                detail = json.loads(detail)["error"]["message"]
            except (ValueError, KeyError, TypeError):
                pass
            raise SystemExit(f"OpenAI {endpoint} failed ({error.code}): {detail}")


def generate(key, model, quality, prompt, size):
    body = json.dumps({"model": model, "prompt": prompt, "size": size, "quality": quality, "n": 1}).encode()
    return request("generations", key, body, "application/json")


def edit(key, model, quality, prompt, size, reference):
    boundary = uuid.uuid4().hex
    parts = []
    for field, value in (("model", model), ("prompt", prompt), ("size", size), ("quality", quality), ("n", "1")):
        parts.append(f'--{boundary}\r\nContent-Disposition: form-data; name="{field}"\r\n\r\n{value}\r\n'.encode())
    parts.append(f'--{boundary}\r\nContent-Disposition: form-data; name="image[]"; filename="reference.png"\r\n'
                 "Content-Type: image/png\r\n\r\n".encode() + reference + b"\r\n")
    parts.append(f"--{boundary}--\r\n".encode())
    return request("edits", key, b"".join(parts), "multipart/form-data; boundary=" + boundary)


# ---- Build ---------------------------------------------------------------

LICENSE = """Theme artwork generated with the OpenAI image API ({model}) from the prompt
recorded in prompt.json. Manifest and palette generated by tools/generate_theme.py.
Released under the repository's MIT License.
"""


def build(args, render):
    out = Path(args.out or ROOT / ".local/generated-themes" / args.id)
    if out.exists():
        raise SystemExit(f"{out} already exists; it will not be overwritten.")
    out.mkdir(parents=True)
    print("Background ...", flush=True)
    background = fit(Image.open(io.BytesIO(render("background", background_prompt(args.prompt), None))), 1920, 1080)
    save_png(background, out / "background.png")
    reference = io.BytesIO()
    background.save(reference, "PNG")

    def tile(app):
        image = fit(Image.open(io.BytesIO(render(app, tile_prompt(args.prompt, app), reference.getvalue()))), 512, 512)
        save_png(image, out / f"{app}.png")
        print(f"Tile {app} done", flush=True)

    with ThreadPoolExecutor(max_workers=3) as pool:
        list(pool.map(tile, APPS))
    (out / "theme.json").write_text(json.dumps(manifest(args.id, args.name, palette(background)), indent=2) + "\n")
    (out / "LICENSE.txt").write_text(LICENSE.format(model=args.model))
    (out / "prompt.json").write_text(json.dumps({"prompt": args.prompt, "model": args.model, "quality": args.quality,
                                                 "background": background_prompt(args.prompt),
                                                 "tiles": {app: tile_prompt(args.prompt, app) for app in APPS}}, indent=2) + "\n")
    validate(out)
    return out


def validate(out):
    env = dict(os.environ, DOTNET_ROLL_FORWARD="Major")
    result = subprocess.run(["dotnet", "run", "--project", str(ROOT / "tools/ThemeTool"), "--", "validate", str(out)],
                            capture_output=True, text=True, env=env)
    if result.returncode != 0:
        raise SystemExit("Theme validation failed: " + (result.stderr or result.stdout).strip())
    print(result.stdout.strip().splitlines()[-1])


def main(argv=None):
    parser = argparse.ArgumentParser(description="Generate a theme pack from one prompt.")
    parser.add_argument("prompt", help="The look you want, e.g. 'misty pine forest at dawn, watercolour'")
    parser.add_argument("--id", required=True, help="Lowercase theme ID, e.g. misty-pines")
    parser.add_argument("--name", required=True, help="Display name, up to 40 characters")
    parser.add_argument("--out", help="Output folder (default .local/generated-themes/ID)")
    parser.add_argument("--model", default="gpt-image-2.5-flare")
    parser.add_argument("--quality", default="medium", choices=["low", "medium", "high", "xhigh", "max", "auto"])
    parser.add_argument("--key-file", type=Path, default=DEFAULT_KEY)
    parser.add_argument("--offline", action="store_true", help="Use flat placeholder art instead of the API (for tests)")
    args = parser.parse_args(argv)
    if not re.fullmatch(r"[a-z0-9][a-z0-9-]{0,47}", args.id):
        parser.error("--id must be lowercase letters, digits and hyphens")
    if not args.name.strip() or len(args.name) > 40:
        parser.error("--name must be 1 to 40 characters")
    if not args.prompt.strip() or len(args.prompt) > 600:
        parser.error("prompt must be 1 to 600 characters")

    if args.offline:
        shades = {"background": (30, 60, 90)}

        def render(kind, prompt, reference):
            buffer = io.BytesIO()
            Image.new("RGB", (64, 36) if kind == "background" else (32, 32), shades.get(kind, (200, 80, 40))).save(buffer, "PNG")
            return buffer.getvalue()
    else:
        key = args.key_file.read_text().strip()

        def render(kind, prompt, reference):
            if reference is None:
                return generate(key, args.model, args.quality, prompt, "2560x1440")
            return edit(key, args.model, args.quality, prompt, "1024x1024", reference)

        print(f"Generating 7 images with {args.model} at {args.quality} quality.", flush=True)
    out = build(args, render)
    print(f"Theme ready: {out}")


if __name__ == "__main__":
    sys.exit(main())
