# Dino Den motion

Sunset-colored smoke drifts above the existing volcano while a small terracotta clay pterosaur glides across the open upper sky. The selected Netflix baby dinosaur closes its visible eye for a brief happy blink. The original diorama, static smoke base, plants, dinosaurs and platform stay intact.

## Production prompts

- `scene/volcano-smoke.png` — Use case: stylized-concept. Use supplied `background.png` only as style/color reference. One isolated horizontal cluster of clay diorama volcano smoke, muted lavender-gray billowing lobes with peach sunset rim light and faint warm orange at lower left. Tactile handmade clay with finger-smoothed irregularity; complete silhouette, transparent margins and feathered translucent edges. No volcano, sky, ground, fire, animals, text, logos, UI, rectangle or checkerboard; genuine transparency.
- `scene/clay-pterosaur.png` — Use case: stylized-concept. Use supplied `background.png` as style reference. One isolated small handmade clay pterosaur gliding right, naturally spread wings in a wide shallow V, readable three-quarter side silhouette, closed beak and short backward crest. Cozy terracotta clay, amber light above and muted lavender shading below, same tactile miniature stop-motion diorama material. Complete wings/tail, transparent margins, horizontal composition. No exaggerated face, props, scenery, ground, text, logos or UI; actual transparency.
- `scene/baby-blink.png` — Use case: precise-object-edit. Edit supplied `netflix.png`: change only the visible large eye to a fully closed curved orange clay eyelid, close far eye if visible. Preserve exact head, body, mouth, teeth, hands, pose, silhouette, texture, ferns, rocks, volcano, sunset, camera, framing and lighting. No black open pupil, new objects, text, logos or UI. Full square artwork aligned for crossfade.

## Preparation and bounds

ImageMagick exports: smoke 768×317 RGBA; pterosaur 512×288 RGBA; blink 512×512 RGB, all metadata stripped. Smoke displays at 430×177, moving (1440,-22) to (1510,-48) in 34 seconds at opacity 0.65. It extends the original static smoke toward the right, rather than requiring a new wallpaper plate. The pterosaur displays at 180×101, traveling (430,80) to (1250,112) in 55 seconds at opacity 0.9; phase 0.34 puts it in the sky at startup. It stays above the central palm canopy and below the upper canvas edge. Two layer rectangles total 94,290 pixels, below 4,147,200. Referenced decoded pixels: 4,299,520, below 16,777,216. Referenced encoded bytes: 6,451,381, below 67,108,864. Largest PNG edge: 1920, including unchanged background. Each individual file is below 16 MiB.

The blink preserves the overall pose but has small generative texture/background differences. Smoke is supplementary to the existing static plume; it is not a fluid simulation. The bird glides without wing deformation. Final TV review should assess smoke overlap and selected-tile crossfade. No background plate replacement or preview field is needed.

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
      "asset": "scene/volcano-smoke.png",
      "x": 1440,
      "y": -22,
      "width": 430,
      "height": 177,
      "toX": 1510,
      "toY": -48,
      "seconds": 34,
      "opacity": 0.65,
      "phase": 0.18,
      "kind": "drift"
    },
    {
      "asset": "scene/clay-pterosaur.png",
      "x": 430,
      "y": 80,
      "width": 180,
      "height": 101,
      "toX": 1250,
      "toY": 112,
      "seconds": 55,
      "opacity": 0.9,
      "phase": 0.34,
      "kind": "travel"
    }
  ],
  "tiles": {
    "netflix": {
      "image": "scene/baby-blink.png",
      "seconds": 7,
      "kind": "blink"
    }
  }
}
```


Shared renderer integration now supports pixel sampling and optional tile animation kinds. Device load/lifecycle results are recorded centrally in [verification](verification.md); the design-task checks above describe asset preparation.
