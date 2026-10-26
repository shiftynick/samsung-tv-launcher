# Cinematic living scene

The approved jungle/sunset wallpaper and all six original icons are unchanged. Three new ImageGen assets provide two drifting mist banks, two distant pterosaur glides, and a selected Apple TV sauropod blink. Generic firefly particles and whole-tile floating are disabled. The original wallpaper's tiny distant flyer and baked-in firefly lights remain static.

## Assets and bounds

| New asset in the Cinema pack | Dimensions | Purpose |
| --- | --- | --- |
| `scene/valley-mist.png` | 1024×384 RGBA | True translucent mist, reused at 1050×270 and 650×200 over the valley and waterfall basin. |
| `scene/pterosaur-glide.png` | 512×256 RGBA | One complete gliding animal, reused at 144×72 and 90×45 for depth. |
| `scene/sauropod-blink.png` | 512×512 RGB | Original Apple TV icon with only the generated closed-eye patch composited over the eye. |

The four native scene rectangles total 427,918 pixels, below 4,147,200. Referenced decoded images total 4,957,184 pixels, conservatively counting each repeated layer reference, below 16,777,216. Unique referenced PNGs total 6,676,250 bytes, below 64 MiB; each file is below 16 MiB. The largest image remains the original 1920×1080 wallpaper. New sprites retain true alpha rather than a keyed background. No new background plate or preview override is needed.

The mist reverses gently over 34/43 seconds with 20%/26% peak opacity. Two phased pterosaurs travel right over 48/61 seconds, with a fixed natural gliding pose rather than wing flapping. They fade at loop boundaries using the existing travel renderer. The selected sauropod blinks every eight seconds. No tile `kind` extension is required: this uses the existing default blink. Existing foreground/Home pause, Off mode, lifecycle and native tile behavior apply.

## ImageGen prompts

All three assets were created with the built-in ImageGen tool, not the CLI. Local reference artwork was visually inspected before generation.

1. **Mist; wallpaper as lighting reference.** “Create only one isolated horizontally elongated low bank of wispy valley mist/waterfall steam, silver blue shadow and delicate warm peach sunlight along the upper curls, matching the existing realistic mist in this exact valley. Fine translucent turbulent strands and separated wisps; very soft feathered edges fading completely to genuine transparency on all sides. Landscape composition, cloud occupies central horizontal half with generous transparent margin. No jungle, landscape, water, dinosaurs, ground, stars, particles, text, logos, frame, or opaque backdrop. Actual alpha throughout the mist; not a solid cloud cutout.”
2. **Pterosaur; wallpaper for atmosphere and `youtube-tv.png` for anatomy/material.** “Create ONE small distant pterosaur gliding to the right, in a natural stable soaring pose, BOTH long membranous wings spread broadly horizontally with slight bank, long pointed beak/crest, feet tucked. Three-quarter underside/side distant view so its right-facing silhouette is readable. Dark charcoal rusty brown leather, restrained amber sunset rim, atmospheric subdued detail to match the tiny flyer already in the wallpaper. Entire animal and both wingtips visible with generous transparent margin. No perch, rocks, jungle, sun, objects from the icon, play symbol, text, logos, or UI. Genuinely transparent background. Wide landscape composition.”
3. **Sauropod blink; `apple-tv.png` as edit target.** “Change ONLY the visible eye of the sauropod to a fully closed believable reptile eyelid with a thin curved horizontal crease in the same small eye socket. Keep the eye-socket scales and eyebrow. Preserve the entire image otherwise exactly: same sauropod identity, head outline, mouth, neck, body, legs, tail, stance, realistic gold/silver scale texture, exact pose and framing, rocks, foliage, mountains, waterfalls, sun, clouds, lighting and colors. No text, logos, or UI. Square output. This is a single brief blink frame, not a new interpretation.”

## Preparation, license and checks

ImageMagick resized the mist to 1024×384, fitted/padded the pterosaur to 512×256 with transparent margins, and resized the generated blink to 512×512. To avoid generative changes elsewhere in the icon, only a feathered 22×22 eye crop at (361,25) was composited over the original; actual changed pixels occupy 21×20 at (362,27). The generated eyelid supplies the artwork; the mask only blends its edge. All production exports have metadata stripped. Raw outputs and review composites remain in ignored local storage.

These generated derivatives use the pack's existing `LICENSE.txt` and project generated-art notices; no third-party downloaded artwork was added. Production PNGs and the assembled scene were visually inspected, including enlarged open/closed-eye comparisons. Working-tree bytes were checked with `tools/audit_public.py` `inspect_blob` without staging. Shared .NET builds and TV validation are intentionally left to the parent integrator. Static inspection cannot establish native animation timing, GPU behavior, or on-TV visibility; the pterosaurs are gliding sprites and the original tiny flyer remains static.


Shared renderer integration now supports pixel sampling and optional tile animation kinds. Device load/lifecycle results are recorded centrally in [verification](verification.md); the design-task checks above describe asset preparation.
