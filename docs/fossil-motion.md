# Fossil Club motion

A low-opacity diagonal museum light drifts across the engraved skeleton and limestone, with small champagne mineral glints carried inside the light. The selected Prime Video blue raptor fossil receives a warm amber raking-light pulse. The skeleton remains inanimate. The logo-free fossil tile was chosen instead of generating another branded amber egg frame.

## Production prompts

- `scene/raking-light.png` — Use case: stylized-concept. Generate one broad diagonal band of warm ivory raking museum light with tiny irregular champagne mineral reflections nested within it, soft feathered edges and translucency, diagonal upper-left to lower-right, wide 3:2 composition. Premium ivory limestone fossil museum style. Light only; no stone slab, skeleton, objects, particles cloud, stars, text, logos, UI, background or checkerboard. Genuine transparent background. The inspected wallpaper informed the art direction; this asset was generated independently without an image attachment.
- `scene/raptor-amber-light.png` — Use case: lighting-weather. Edit supplied `prime-video.png`: change only illumination to a subtle warm amber museum spotlight across upper skull and ribcage, restrained mineral sheen on raised blue bone edges. Preserve silhouette, engraving, bones, ferns, cracks, framing and camera. Blue fossil stays blue and limestone ivory. Inanimate skeleton, no blinking or movement, no new objects, text, logos or UI; square full bleed matching original geometry for crossfade.

## Preparation and bounds

ImageMagick exports: light 960×640 RGBA; alternate 512×512 RGB, both metadata stripped. The light is displayed at 1440×960 with peak opacity 0.14; its origin moves (450,-170) to (620,-110) over 48 seconds. Upper crop keeps the light source outside the canvas. One scene layer occupies 1,382,400 rectangle pixels, below 4,147,200. Referenced decoded pixels: 4,523,008, below 16,777,216. Referenced encoded bytes: 8,614,534, below 67,108,864. Largest PNG edge: 1920, including unchanged background. Each individual file is below 16 MiB.

The light is intentionally restrained on ivory, so the TV review should assess visibility at Subtle. The alternate retains the composition but has small generative texture/engraving differences; the pulse is a lighting illustration, not a pixel-identical relight. No background plate replacement or preview field is needed.

## Integration and license

Built-in ImageGen was used for every new raster asset; no CLI or code-generated raster substitute was used. Local references were inspected before generation. ImageMagick only resized the generated outputs and stripped metadata; sprite alpha was retained. Every production PNG and a composite over the original background were visually inspected. No original background, icon, palette, or license was changed. New art contains no text, logos, or UI. Generated assets follow the existing pack `LICENSE.txt` (MIT) and repository notices; no downloaded publisher art or private references were used.

Generic dust is disabled (`effect: none`, `count: 0`) and whole-tile float is disabled. Scene movement uses the existing native layer renderer. Tile motion uses the shared renderer's optional `motion.tiles.*.kind` support (`pulse` for material lighting, `blink` for character eyelids). Older loaders that accept only `image` and `seconds` must be updated first. No shared renderer code was changed here. Only the selected Home tile should animate. Off, foreground/Home pause, page/theme disposal, and device performance remain the parent integration checks. No shared dotnet build or TV operation was run.

## Exact motion data

```json
{
  "effect": "none",
  "count": 0,
  "seconds": 18,
  "iconFloat": false,
  "layers": [
    {
      "asset": "scene/raking-light.png",
      "x": 450,
      "y": -170,
      "width": 1440,
      "height": 960,
      "toX": 620,
      "toY": -110,
      "seconds": 48,
      "opacity": 0.14,
      "phase": 0.22,
      "kind": "drift"
    }
  ],
  "tiles": {
    "prime-video": {
      "image": "scene/raptor-amber-light.png",
      "seconds": 12,
      "kind": "pulse"
    }
  }
}
```


Shared renderer integration now supports pixel sampling and optional tile animation kinds. Device load/lifecycle results are recorded centrally in [verification](verification.md); the design-task checks above describe asset preparation.
