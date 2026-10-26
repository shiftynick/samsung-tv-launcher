# Experience improvements under investigation

These are research findings and proposed next steps, not TV changes or completed features. The repository is now named Samsung TV Launcher; the initial dinosaur designs are bundled themes rather than the project's identity.

## OLED optimization

Samsung documents a short Screen Optimization cycle after at least four cumulative viewing hours. It normally runs when the TV is turned off with the remote while still connected to power, and takes about ten minutes. Turning the TV back on or unplugging it before completion cancels the cycle. Samsung also documents a prompt to optimize immediately after twelve unoptimized viewing hours.

Source: [Samsung's Screen Optimization guide](https://www.samsung.com/hk_en/support/tv-audio-video/how-to-use-screen-optimization-on-a-samsung-oled-tv/) and [image-retention guidance](https://www.samsung.com/levant/support/tv-audio-video/how-to-troubleshoot-image-retention-on-your-samsung-oled-tv-screen/).

This is panel maintenance intended to reduce image retention, not advertising or analytics. The first diagnostic is to leave the TV powered at the wall but off through the remote for at least fifteen uninterrupted minutes. This may clear an overdue cycle; it is not a guarantee about any particular firmware fault. If forced shutdowns continue despite completed maintenance, inspect cycle-completion state, uptime/power events and firmware-specific panel-care settings read-only before considering a change.

No supported off switch for this exact firmware has been established. No protection has been disabled, no completion counter falsified, and no undocumented service-menu adjustment is proposed. A possible future feature is a read-only maintenance-status/reminder screen if a reliable status interface is found. It must distinguish verified completion from an elapsed timer and preserve panel care.

## Performance

Measure current foreground/background processes, CPU, available memory, graphics-memory pressure, startup and visible remote/app latency before changing services. Installed packages are not necessarily running processes. Compare the same app/workload before and after one reversible change.

Candidates include verified unused startup/preload work, unwanted TV Plus or optional voice/integration services, and excessive launcher preview decode sizes. Keep the six selected apps, accounts, app frameworks, DRM, HDMI, audio, display/OLED care, networking, Bluetooth and accessibility. Do not apply the upstream U8000F disable list to an S90D without inspecting actual units/dependencies.

The launcher already uses local artwork, a bounded active-theme image cache, and stops its clock timer when paused. A permanent process-killer, broad cache wipe, disabled security updates, or an invented speedup percentage is not part of this plan. Clear a particular app's cache only when justified, preserving account data.

## Privacy

Start with documented consent controls: Viewing Information Services/ACR, interest-based advertising and other unwanted optional personalization/voice services. [Samsung's ACR explanation](https://www.samsung.com/us/support/answer/ANS10010616/) and [Smart TV privacy notice](https://www.samsung.com/us/info/privacy/smarttv/) describe the distinction between optional viewing recognition and other Samsung/third-party collection.

Next, capture a private network/DNS baseline and validate opt-outs. If needed, add narrowly selected DNS/firewall rules and separately verified service changes, with rollback and tests for every streaming app, app updates, authentication, DRM and HDMI. Blocking all Samsung destinations can break useful functions. A clean Home screen is not evidence that background tracking is stopped; a missing process is not a decrypted traffic audit. Raw viewing/account/device traffic remains private and must never be published as an issue attachment.

No S90D privacy cleanup or traffic audit has been performed yet.

## Theme packs

Current implementation: four hardcoded theme IDs/display names; theme-specific palette/radius/font/sampling branches in Home.cs; a four-choice picker; PNG art in per-theme folders. Current artwork is independently replaceable, but arbitrary new themes are not discovered automatically.

Proposed format: a folder containing `theme.json`, a wallpaper, optional preview/font/license files and app artwork indexed by stable keys such as `plex`, `netflix` and `youtube-tv`. A manifest would declare ID, name, palette, text colors, focus treatment, corner radius, font, image sampling and animation preference. New themes would be discovered at runtime and paginated in the picker, with no C# rebuild for ordinary art/palette changes.

Proposed validation: unique IDs, schema version, bounded image dimensions and total decoded memory, sufficient required assets or explicit fallbacks, safe relative paths, known style values, and a default neutral theme if a pack is missing/corrupt. Keep theme data separate from app IDs, HDMI/source configuration and device-specific access profiles. Theme packs contain no executable scripts or native code. Include a validator and starter template so contributors can build their own pack without owning the original TV.

The initial implementation can support authorized host-side installation/reload. USB import, online theme downloads, a marketplace and arbitrary code-defined layouts are separate features, not promises for the first manifest format. Existing dinosaur themes remain available, but a neutral starter should make the generic project useful without dinosaur art.
