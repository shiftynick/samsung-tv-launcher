# Custom apps, games and future experiments

The current launcher is a native .NET/NUI application. A small built-in game or utility can be added as a separate native screen with its own input, rendering and pause/resume lifecycle. That is a source change and rebuild, not a theme-pack feature. Themes remain data-only.

Larger projects fit better as separate Tizen apps with ordinary launcher tiles. Samsung documents HTML/CSS/JavaScript TV web apps, .NET TV samples and gamepad input. They need their own device testing, permissions, signing/development installation and lifecycle handling. Root access does not automatically bypass package/signing requirements or make Android APKs compatible. The existing launcher can launch an installed app ID; arbitrary app installation is not implemented by the theme tool.

Sources: [TV web-app quick start](https://docs.stg.tizen.org/application/web/get-started/tv/first-app/), [.NET TV samples](https://developer.samsung.com/smarttv/develop/samples/tizen-net-tv-samples.html), [gamepad guide](https://developer.samsung.com/smarttv/develop/guides/user-interaction/gamepad.html).

## Promising small projects

- Remote-first games: Snake, sliding puzzles, memory matching or trivia, with local scores.
- A local-only dashboard: clock, weather from a chosen provider, optional smart-home controls, or a photo gallery. Each network/account integration needs explicit setup; never bake tokens into public source.
- Launcher editing: reorder/hide tiles, friendly HDMI names, controller-friendly navigation and per-theme motion choices.
- An idle mode that reduces bright static UI and retains the TV's native screen saver and OLED care. Small decorative animations are not burn-in protection.
- A private diagnostics screen for app launch timing, memory pressure and confirmed maintenance status, if reliable APIs are identified.

## Privacy and performance sequence

The S90D inventory found active advertising/analytics/recognition-related units and enabled app preloading. It did not establish ACR consent or actual transmitted content. Recent memory-pressure averages were zero; compressed swap occupancy alone was not evidence of a problem. No system-service cleanup or preload change has been applied.

Start with Viewing Information Services/ACR and other optional advertising/voice consents. Samsung documents ACR as optional and allows changing it through Settings. Next, take a private DNS/network baseline and compare opt-out effects, then evaluate a narrow reversible service/preload experiment. Preserve streaming authentication, DRM, updates, HDMI, sound, networking, Bluetooth, accessibility and OLED care. Measure startup/app latency before promising performance gains. DNS-only filtering has blind spots and broad Samsung blocking can break required features.

Source: [Samsung ACR explanation](https://www.samsung.com/us/support/answer/ANS10010616/). See [the roadmap](roadmap.md) for the OLED-maintenance findings and [verification](verification.md) for tested scope.
