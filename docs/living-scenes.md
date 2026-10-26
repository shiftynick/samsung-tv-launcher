# Living scene layers

Neon District's second motion pass moves actual scene artwork: three hovercars travel through the city, two translucent mist layers drift near the waterfront, and the selected Netflix robot cat blinks. The generic rain particles are disabled for this pack. All four dinosaur packs now have matching scene artwork as well: [Cinematic](cinema-motion.md), [8-bit Jungle](pixel-motion.md), [Fossil Club](fossil-motion.md) and [Dino Den](den-motion.md). Their generic particles and whole-tile float are disabled.

The renderer remains native NUI. It uses fixed PNG textures, looping property animations and a single selected-tile alternate frame, not video playback or a per-frame managed timer. Off removes all layers/alternate frames. Scene/tile motion runs only on Home in the foreground, pauses behind other apps, and is disposed when changing page/theme. Motion modes remain independent of the selected theme. These decorative effects do not replace OLED care or an idle screen saver.

## Data format

Within `motion`, `layers` is an ordered array (drawn over the background and behind native tiles). Each entry may declare a PNG `asset` or a native solid `color`, its `x`, `y`, `width`, `height`, target `toX`, `toY`, loop `seconds`, peak `opacity`, initial `phase` in [0,1), and `kind`:

- `travel`: one-way movement, fading out before the loop wraps.
- `drift`: reversible movement with a slow opacity cycle.
- `pulse`: the same keyframe mechanism for fixed-position light/opacity changes; keep start and target coordinates equal for a stationary light.

Coordinates refer to the 1920×1080 canvas. At most eight layers are allowed, each at most one screen in size; their combined rectangle area is capped at two screens. Duration is 3–90 seconds. Positions are bounded, opacity is 0–1, unknown fields are rejected, and referenced PNG files share the pack's existing file/path/pixel budgets. Transparent assets must retain alpha. `phase` staggers traffic so cars are visible at startup rather than waiting through an empty loop.

`tiles` maps an existing app icon key to an alternate PNG `image` and a 3–20 second cycle `seconds`. The optional tile `kind` is `blink` (the default brief eyelid crossfade), `pulse` (slow blend into a light/material alternate), or `flutter` (two alternating holds per cycle). Unknown kinds are rejected. Only the selected Home app creates an alternate view. It works independently of `iconFloat`; Neon District disables the old whole-tile float. Matching composition between the two frames matters more than extra detail. Scene and tile image sampling follows `pixelArt`, so pixel packs use nearest-neighbor sampling rather than softened interpolation.

See the [Neon District manifest](../launcher/assets/app-icons/themes/cyberpunk/theme.json) for a complete example. The shared ThemeTool validates and clones the referenced scene/frame assets too. Adding these fields to an installed older launcher requires updating the loader first, since it deliberately rejects unknown fields.

## Artwork and prompts

Four assets were generated using the built-in ImageGen tool, with the existing wallpaper or robot cat as references. Originals remain unchanged. Final exports are under `launcher/assets/app-icons/themes/cyberpunk/scene/`:

| File | Production prompt |
| --- | --- |
| `city-plate.png` | Edit the supplied cyberpunk wallpaper: remove only all airborne hovercars/exhaust streaks, fill naturally with sky/city/mist, preserve composition, buildings, moon, bridges, palette and quiet dark rooftop; no new objects, text or UI. |
| `hovercar.png` | Extract/design one isolated matching cyan/magenta hovercar, horizontal three-quarter side view facing right, midnight metal, cyan front lights and short magenta engine glow; complete silhouette with generous true transparent margins, no background, ground, text or extra vehicles. |
| `mist.png` | A standalone elongated bank of soft silver-blue atmospheric mist with faint lavender/cyan reflected neon, genuine alpha throughout and feathered transparent edges; no city, objects, solid background or border. |
| `cat-blink.png` | Edit the supplied square red robot cat: change only both amber eyes to fully closed eyelids; preserve pose, head/body outline, ears, tail, whiskers, buildings, moon, camera, framing and lighting. |

Deterministic ImageMagick exports resize the plate to 1920×1080, hovercar to 512×288, mist to 1024×576 and blink frame to 512×512; they strip metadata and preserve sprite alpha. The first three form new animation layers rather than overwriting the original wallpaper. The alternate cat frame was visually checked against the original; small generative differences mean it is an illustrated blink rather than a pixel-identical eye-only edit. Assets use the pack's existing generated-art license and notice.

## Device checks

The recorded S90D loaded all five scene textures plus the selected cat frame, with no reported motion error. Off, picker cleanup, background pause/frame release, return and process-restart persistence were checked. Native Home routing stayed unchanged. The owner confirmed the visual direction. GPU timing and streaming playback after this update remain separate checks; see [verification](verification.md).
