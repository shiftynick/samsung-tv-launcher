# Bundled themes

The Themes control discovers packs at runtime. The four dinosaur styles each include a title-free 1920×1080 wallpaper and six independent square illustrations: amber egg/chevron (Plex), red T-rex skull or baby T-rex (Netflix), pale sauropod (Apple TV), cyan raptor (Prime Video), red pterosaur/play motif (YouTube), and a separate flying reptile/television motif (YouTube TV).

| Theme | Direction |
| --- | --- |
| Cinematic | Misty emerald jungle, sunset sauropod and amber accents; detailed realistic illustrations |
| 8-bit Jungle | Twilight jungle, visible square pixels, limited palette, pixel font and nearest-neighbor sampling |
| Fossil Club | Ivory limestone, engraved skeletons, mineral-colored fossils and terracotta focus |
| Dino Den | Clay dinosaur dioramas, warm island sunset and friendly sculpted characters |
| Neon District | Midnight cyberpunk city, cyan/magenta/amber lighting and six futuristic app illustrations |

Assets live in `launcher/assets/app-icons/themes/{cinema,pixel,fossil,den}/`. App names are not embedded in wallpapers. Focus borders, app labels, clock and controls are rendered by NUI. The preference is saved only in the launcher's own writable data directory; unknown or corrupt values fall back to Cinematic or another valid pack; an embedded flat recovery is used only if no packs are valid. Save uses an adjacent temporary file and atomic replacement. Image views for inactive themes are released; the cache is bounded to the active theme.

For authoring, validation and installation without a C# rebuild, see [theme packs](theme-packs.md). The picker paginates beyond four choices. Native motion is controlled separately on Home; per-pack effects are declared in theme.json.

Neon District uses seven independently generated images; its prompts and asset notes are in [cyberpunk theme](cyberpunk-theme.md).

## Reproduction prompts

The art was generated with the built-in ImageGen tool, then prepared for native UI use. These are sanitized production prompt specifications rather than private conversation transcripts.

**Cinematic background:** Full-bleed 16:9 prehistoric jungle at blue-hour; emerald ferns, mist, distant long-neck dinosaur at amber sunset, waterfall and subtle fireflies. Dark calm lower foreground. No words, logos, app tiles, clocks, borders or controls.

**8-bit background:** A late-1980s-style pixel-art prehistoric jungle at twilight, layered dark teal ferns, purple mountains, amber sun, blocky waterfalls, brachiosaur silhouettes and pterosaurs. Chunky square pixels and limited palette. No interface, text, clock, smooth painted details or border.

**Fossil background:** Warm ivory limestone with a large engraved T-rex skeleton in the upper/right area, delicate geological landscape and botanical fossil engravings, subtle terracotta strata, quiet lower-middle region. No headings, slogan, app cards, clock, lines or controls.

**Dino Den background:** A premium tactile clay prehistoric island at lilac/peach sunset, distant volcano, jungle islands, waterfalls, ocean and tiny friendly dinosaurs at the edges. Natural mossy rocks replace all letter-shaped signage; quiet foreground stone ledge. No words, labels, clock or UI.

**App sheets:** Exactly three columns by two rows of equal square cells, edge-to-edge with no gutters or UI border. Draw the six subjects listed above in row-major order, keeping each fully inside its own cell with safe margins. Match the chosen theme's material and palette. No names, wordmarks, selection highlights or rounded framing. Crop cells independently for the native renderer. Pixel tiles are quantized and nearest-neighbor resized; other art retains detailed continuous tones.

Generated PNGs are included so reproducing the exact live art does not require another generation. Brand references identify the corresponding apps; they do not imply affiliation or trademark rights. See [NOTICE.md](../NOTICE.md).
