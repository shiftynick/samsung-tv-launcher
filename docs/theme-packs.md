# Create and add theme packs

Theme packs are data folders, discovered at runtime. Adding art or a palette does not require rebuilding the TV launcher. The initial installation of a launcher version supporting packs is still required. Five illustrated packs ship with it: Cinematic, 8-bit Jungle, Fossil Club, Dino Den and Neon District. The picker paginates automatically.

## Create, clone and validate

The host tool shares the same C# parser and validation rules as the TV, and needs no proprietary Samsung references. A .NET SDK is required on the host. If your host has a newer runtime but no .NET 6 runtime, use `DOTNET_ROLL_FORWARD=Major` for these host commands; this does not change the TV runtime.

```sh
# A new neutral, label/monogram-based starter with no dinosaur art.
dotnet run --project tools/ThemeTool -- create my-theme "My Theme" .local/my-theme

# Or clone a bundled style, including only its referenced art/font/license files.
dotnet run --project tools/ThemeTool -- create my-cinema "My Cinema" .local/my-cinema launcher/assets/app-icons/themes/cinema

# Edit the folder's theme.json and PNG artwork, then validate.
dotnet run --project tools/ThemeTool -- validate .local/my-theme

# List/discover packs and report malformed or duplicate packs.
dotnet run --project tools/ThemeTool -- list launcher/assets/app-icons/themes
```

Create refuses an existing output directory. A clone retains the base pack's licenses; use artwork and fonts you have permission to distribute. App IDs and HDMI settings are separate from themes and must not be placed in theme files.

## Manifest

A minimal pack requires only a schema version, ID and display name. Missing imagery uses native app monograms and a flat palette background.

```json
{
  "schemaVersion": 1,
  "id": "my-theme",
  "name": "My Theme",
  "order": 100,
  "background": "background.png",
  "icons": {
    "plex": "icons/plex.png",
    "netflix": "icons/netflix.png",
    "apple-tv": "icons/apple-tv.png",
    "prime-video": "icons/prime-video.png",
    "youtube": "icons/youtube.png",
    "youtube-tv": "icons/youtube-tv.png"
  },
  "palette": {
    "background": "#101918",
    "text": "#FAF5E6",
    "muted": "#C7CFC9",
    "accent": "#FFC247",
    "card": "#081416",
    "control": "#081416D9",
    "caption": "#031016E6",
    "overlay": "#041018EB",
    "header": "#00000038",
    "footer": "#000000B3"
  },
  "cornerRadius": 18,
  "pixelArt": false,
  "animationMs": 160,
  "fontFamily": "Quiet Inter",
  "fontVariants": true,
  "license": "LICENSE.txt"
}
```

Omit `background` and `icons` to use the starter without images. Individual missing icon mappings fall back to app monograms, while labels remain native text. If an image is declared, the file must exist and pass validation. `preview` optionally points to a separate PNG for the picker; otherwise it uses the background. A missing/corrupt selected pack falls back to Cinematic when available, then the first valid pack. Only when no valid packs remain does an embedded flat recovery screen appear; a plain theme is not included in normal selection.

Colors use `#RRGGBB` or `#RRGGBBAA`. Use high-contrast text and test the visible result; the parser checks syntax, not perceptual contrast. `pixelArt` selects nearest-neighbor sampling and compact pixel typography; set animationMs to zero for no focus animation. `fontVariants: true` uses the bundled family suffixes Medium/Semibold/Display. For a single custom font set it false, declare its exact family name, and include `fontFile` plus `fontLicense` relative paths. Fonts must be TTF; their licenses remain separate. A theme's optional `license` file is included when cloning/exporting.

## Motion

Optional native effects are declared as data, with no video player or pack scripts:

```json
"motion": {
  "effect": "fireflies",
  "count": 10,
  "seconds": 18,
  "iconFloat": true
}
```

Effects: `none`, `fireflies`, `dust`, `rain`, or `stars`. They use small native colored shapes and position/opacity loops; they do not animate individual objects inside a flattened wallpaper. Count is bounded to 0–18 and loop duration to 8–60 seconds (individual loops are staggered by up to four seconds). Subtle shows at most six particles; Full shows up to eighteen. The selected app icon floats by two/four pixels when enabled. Pixel packs may opt out of icon float.

Home's Motion control cycles Off / Subtle / Full, stored independently of theme selection. Off also disables animated focus transitions. Ambient/icon loops run only on Home while foregrounded, pause behind apps/HDMI, and are disposed on page/theme changes. No per-frame managed timer or network asset loading is used. `animationMs` controls focus transitions separately; set Motion Off to disable all animation. Existing packs without a motion block get no ambient particles.

## Bounds and rejection

- Unique lowercase IDs of up to 48 characters, schema version 1, display names up to 40 characters, and at most 64 discovered packs. An embedded recovery screen is used only when none are valid.
- PNG images only: maximum edge 3840, maximum individual pixels 8,294,400, maximum file size 16 MiB. Total referenced image pixels must not exceed 16,777,216 (64 MiB at four bytes/pixel); unique encoded image files must not exceed 64 MiB.
- The PNG checker checks headers/chunk structure and bounds; native decoding still determines whether image data renders. Deleted images fall back safely. Preview decode dimensions are explicitly capped in the UI.
- Manifests at most 64 KiB; fonts at most 2 MiB; license files at most 64 KiB. Corner radius is 0–40 and animation duration 0–250 ms.
- No absolute/network paths, traversal, symlinks, executable hooks, unknown/duplicate fields or duplicate theme IDs. Paths use forward slashes and portable ASCII filename segments.

The host validator does not fetch URLs or execute pack code. There is no online marketplace or arbitrary layout scripting.

## Install without a launcher rebuild

Bundled packs are under the existing app's `res/quiet-home/app-icons/themes/`. Additional owner-installed packs may live under the application's writable data directory in `themes/`. On the recorded S90D that directory was `/opt/usr/home/owner/apps_rw/com.samsung.tv.bixby-promotion/data/themes/`; verify your actual app data path and its owner/SMACK label before writing there. Existing IDs cannot be shadowed by a second pack; use a new unique ID.

Use the explicit-target, verified access procedure in [S90D setup](10-s90d-setup.md). Validate the source pack, export it to a new clean directory using the create/clone command, and transfer only that directory's theme.json and referenced assets. Retain the existing theme/preference and prepare rollback before replacing any files. Do not extract an arbitrary downloaded archive on the TV. Preserve the original owner/permissions/SMACK labels on the chosen resource or app-data directory.

Open Themes to refresh discovery. New packs appear and changed definitions/art are reloaded when selected, with no C# rebuild or TV restart. The app keeps only active-theme artwork; unselected packs are not all preloaded. Existing theme choices remain valid by ID. Restarting the launcher may be required after replacing a font already registered under the same family name.

For a first installed pack, rollback is to select an existing pack, remove only the new pack directory, and reopen Themes. For updates, restore the saved previous folder and preference. Do not alter Samsung's Home routing, package registry or streaming accounts merely to change a theme.
