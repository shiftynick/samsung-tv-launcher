# 8-bit Jungle living scene

This data-only update preserves the approved `background.png` and all six base icons. Three new PNGs live in `launcher/assets/app-icons/themes/pixel/scene/`. Generic stars are disabled (`effect: none`, `count: 0`) and whole-tile floating remains disabled.

## Effects and bounds

| Asset | PNG dimensions | Use |
| --- | --- | --- |
| `scene/sunset-cloud.png` | 328 × 56 | Two slow coral/purple cloud drifts; (650,58) → (890,58), 72 s, opacity .64, phase .2; (1390,236) → (1170,236), 84 s, opacity .48, phase .65. |
| `scene/pterosaur.png` | 168 × 96 | Left-facing navy/orange flying reptile travels (1810,4) → (580,12), 46 s, opacity .94, phase .38. |
| `scene/raptor-blink.png` | 136 × 136 | Selected `prime-video` cyan dinosaur briefly closes its eye every 7 s, using the default blink animation. |

Three layer rectangles total 52,864 pixels, below 4,147,200. Referenced decoded pack pixels total 2,255,936 (the reused cloud is conservatively counted twice by the current parser), below 16,777,216. All asset edges are below 3840 and encoded assets are far below the 64 MiB pack budget. Cloud and pterosaur have binary true alpha and four-pixel transparent margins, with no checkerboard or backdrop. Tile frame is opaque like its original.

PixelArt must apply nearest-neighbor sampling to scene layers and alternate frame images, as well as the existing wallpaper/icons. No optional tile `kind` extension is required. The scene uses existing native `drift` and `travel` behavior, including travel's fade before reset. Foreground Home pause/disposal and Off/Subtle/Full handling remain renderer responsibilities. No shared code was changed or built here.

The original wallpaper's three distant flying silhouettes remain in place; the moving pterosaur is an additional creature. Its route stays near the top of the sky. Clouds are sparse, subdued additions rather than a replacement sky. There is no wing-frame animation; the scene creature glides.

## Generation and preparation

All source art came from the built-in ImageGen tool; no CLI fallback or code-drawn raster substitute was used. Local references were visually inspected before generation. The wallpaper was a style/palette reference for the two sprites; the cyan dinosaur icon was an edit target for the blink. No third-party publisher art was fetched. These generated derivatives use the pack's existing MIT license in `LICENSE.txt`; font licensing remains separate and unchanged.

Production prompts:

- **Pterosaur — stylized-concept:** Isolated transparent pixel-art game scene sprite. Reference image is style and palette only; do not reproduce the whole scene. One flying pterosaur facing left, complete silhouette, wings extended in a shallow upstroke, tiny tucked legs, long beak and subtle crest. Match reference's navy/indigo silhouette, muted blue block highlights and orange sunset edge. Strict chunky 8-bit pixels, stepped contours, six-color limited palette, no antialiasing or smooth shading. Readable at 160 × 88, generous transparent margins. Genuine alpha; no checkerboard, scenery, ground, shadow, text, logo, UI, stars or particles. Travels across upper sunset sky.
- **Cloud — stylized-concept:** One transparent pixel-art cloud sprite for sunset jungle. Reference is style and palette only. One thin elongated loose cloud bank, horizontally layered rectangular clusters in muted coral, orange and dusty purple, matching distant blocky clouds. Sparse gaps, complete cloud and transparent margins. Strict 8-bit blocks, stepped contours, five flat colors, no antialiasing, blur, glow or gradients. True alpha outside and between clusters. No opaque backdrop, checkerboard, sun, ground, jungle, dinosaurs, extra objects, text, logos or UI.
- **Blink — precise-object-edit:** Alternate blink frame of supplied 136 × 136 cyan dinosaur icon. Change only small eye into closed dark horizontal eyelid. Preserve species, pixel silhouette, pose, jaw, limbs/claws, tail, colors, position, size, framing and dark square background. Same coarse pixel grid, no smoothing, antialiasing or extra detail. Perfectly aligned frame; no text, logo, UI, borders, glow, scenery or new objects. Do not redesign creature; only tiny eye area changes.

ImageMagick production preparation:

- Scene sprites: threshold existing generated alpha at 50%, trim transparent exterior, reset page; nearest-neighbor resize pterosaur to 80 × 44 and cloud to 160 × 24; quantize without dithering to 10/8 colors respectively; nearest-neighbor enlarge exactly 2×; add four transparent pixels on every side; strip metadata. This keeps source-generated shapes and gives each visible block a crisp two-pixel minimum grid.
- Blink: nearest-neighbor resize generated frame to 136 × 136. Its body had small unwanted generative differences, so crop only the 9 × 6 eye patch at (102,19), remap that patch without dithering to the original icon palette, and composite at the same coordinates onto a copy of the untouched original. Only that eye region may differ. Strip metadata.

All three production exports were visually inspected, including a temporary scene composite and enlarged base/frame comparison. Sprite alpha, dimensions, palette, budget and working-tree public-blob audit were checked locally. Raw generated art and inspection composites remain private/ignored. No staging, commits, shared builds, device, Photos or firewall actions were performed. Parent integration must validate the shared parser and nearest-neighbor renderer, and inspect actual motion, tile blink and pause behavior on TV; timing and visual smoothness are not hardware-verified here.


Shared renderer integration now supports pixel sampling and optional tile animation kinds. Device load/lifecycle results are recorded centrally in [verification](verification.md); the design-task checks above describe asset preparation.
