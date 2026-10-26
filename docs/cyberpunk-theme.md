# Neon District

Data-only theme pack: `launcher/assets/app-icons/themes/cyberpunk/`. Manifest identity is `cyberpunk`, display name **Neon District**, order **50**, schema version **1**. It uses the existing Quiet Inter font family and native app labels, focus, menus and navigation.

The built-in OpenAI ImageGen tool generated seven separate images, rather than an app sheet. No downloaded publisher artwork or device captures were used. The wallpaper is a title-free neon city with a dark, calm rooftop foreground for native tiles. Cyan focus, pale text and midnight navy surfaces complement the magenta and amber lighting.

## Production prompts

**Wallpaper:** Use case: stylized-concept. Asset type: native Samsung TV launcher wallpaper, full bleed 16:9 landscape intended for 1920x1080. Primary request: polished cinematic cyberpunk Neon District at blue hour after rain, playful futuristic mood. Scene: layered skyline of imaginative towers, elevated rail curves, tiny flying transit pods, roof gardens, luminous cyan and magenta architectural light strips and a few warm amber windows; a large soft atmospheric moon behind distant towers. Composition: skyline and interesting details concentrated in upper 55 percent, panoramic depth; lower 45 percent a very dark quiet midnight navy rooftop foreground with only faint reflections, ample calm negative space for native app tiles. Sophisticated painterly 3D concept illustration, beautiful atmospheric lighting, restrained bloom, crisp silhouettes. Palette midnight navy, electric cyan, orchid magenta, warm amber. Text: none. No letters, numbers, glyphs, signage, logos, trademarks, app icons, UI, buttons, cards, borders, watermarks or people. Architecture must not resemble typography. Render a single continuous wide scene.

Each independent square illustration used this template, substituting one primary request from the table:

> Use case: stylized-concept. Asset type: one independent square app illustration for a native TV launcher. Primary request: [subject below]. Style: polished premium painterly 3D cyberpunk concept illustration matching a fun neon city district, rich midnight navy shadows, restrained luminous bloom, tactile brushed metal and colored glass, sophisticated crisp silhouette. Composition: one clear centered hero subject occupying about 70 percent of the square, entire subject comfortably inside frame with safe margins; atmospheric softly blurred distant futuristic city behind, strong readability at small tile size, full bleed edge-to-edge art. Text: none. No letters, numerals, glyphs, signage, app logos, brand marks, play symbols, UI, cards, buttons, borders, watermark or people. This is an evocative illustration, not a logo or interface. Square aspect ratio.

| Stable app key | Primary request |
| --- | --- |
| `plex` | a charming amber-glass holographic film projector orb, floating luminous film loops around its core, on a small futuristic rooftop pedestal; amber dominant with cyan and magenta rim lights |
| `netflix` | a sleek crimson and magenta robotic street cat with luminous amber eyes, sitting confidently on a rain-slick rooftop ledge, cyan rim light; red-magenta dominant |
| `apple-tv` | an elegant pearlescent silver miniature orbital observatory, smooth sculptural spherical dome opening toward a glowing cyan celestial orb, floating over a midnight rooftop; cool white-silver dominant, magenta and amber accent lights |
| `prime-video` | a streamlined cyan courier hover-bike with luminous turbine rings, hovering over a midnight city landing pad, playful cinematic futuristic vehicle design; cyan-blue dominant, amber and magenta accent lights |
| `youtube` | a playful red-orange retro-futuristic video camera drone with a large round lens and two small rotor pods, hovering against a dark city rooftop; coral red and amber dominant, cyan rim light |
| `youtube-tv` | a sculptural magenta rooftop broadcast satellite dish with concentric luminous cyan signal arcs and a small amber satellite overhead, on a midnight city roof; violet-magenta dominant with cyan and amber accents |

## Preparation and validation

ImageMagick deterministically resized the generated wallpaper to **1920×1080** and each independent illustration to **512×512**, matching the existing pack sizes. The generated images already had the intended aspect ratios; no art was painted or assembled through code. The production export uses `-resize`, `-strip` and `PNG24:` to remove nonvisual metadata and retain RGB PNG artwork. Raw generator outputs are not part of the pack.

Validate through the shared host/TV parser from the project root:

```sh
DOTNET_ROLL_FORWARD=Major dotnet run --project tools/ThemeTool -- validate launcher/assets/app-icons/themes/cyberpunk
```

Validation returned `Valid: cyberpunk — Neon District`. All seven production PNGs were visually inspected after export. The public audit's `inspect_blob` check passed for all 11 new files using their working-tree bytes; no staging was performed. Each PNG contains only `IHDR`, `IDAT` and `IEND` chunks. The seven images total 3,646,464 decoded pixels and 5,502,851 encoded bytes, within the shared parser's pack budgets. The installed SDK emitted its existing net6.0 support warning; validation exited successfully.

The first integration added native rain particles; the [living-scene update](living-scenes.md) replaces those with moving hovercars, drifting mist and a robot-cat blink. The wallpaper and icons remain static PNGs. The launcher update checked all artwork ready and no reported motion error on the recorded S90D; visible review remains separate.

The theme includes `LICENSE.txt` following the bundled generated packs and an artwork-specific `NOTICE.md`. Generated outputs may not be unique; app names and trademarks remain their owners' property. The design task itself did not access the TV; integration evidence is recorded separately in [verification](verification.md).
