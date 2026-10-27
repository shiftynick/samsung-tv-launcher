# Theme generator

`tools/generate_theme.py` turns one sentence into a complete theme pack: a 1920×1080 background, six matching 512×512 app tiles and a palette taken from the background. The pack is checked with the same ThemeTool parser the TV uses. App names remain native launcher text; prompts forbid lettering and logos in the art.

```sh
python3 tools/generate_theme.py "retro 80s synthwave beach" --id synthwave-beach --name "Synthwave Beach"
```

Requirements on the host: Python with Pillow, a .NET SDK (for validation; the tool sets `DOTNET_ROLL_FORWARD=Major` itself) and an OpenAI API key with image access. The key is read from `~/.config/tv-theme-generator/openai-key` (or `--key-file`) and is only sent to OpenAI.

## What it does

1. Generates the background at 2560×1440 with a composition brief: interest in the upper part, a calm darker foreground where the tiles and text sit. It is scaled and cropped to 1920×1080, since the TV draws its interface at 1080p even on a 4K panel.
2. Generates each tile as an edit of the finished background, so all six share its style and lighting. Each tile gets a different subject and the app's familiar colour (for example crimson for Netflix, sky blue for Prime Video) instead of a brand mark.
3. Picks a dark base for panels and the most vivid bright colour for the accent, then writes `theme.json`, `LICENSE.txt` and `prompt.json`, which records the exact prompts and model.
4. Re-encodes every PNG, which drops any embedded metadata from the API output.

Options: `--model` (default `gpt-image-2.5-flare`), `--quality` (default `medium`), `--out` (default `.local/generated-themes/ID`, which stays private) and `--offline`, which uses flat placeholder art for tests. Output folders are never overwritten.

A generated pack is a normal data-only pack: copy it into the launcher's user theme folder on the TV, or under `launcher/assets/app-icons/themes/` to bundle it. Review the art before publishing it.
