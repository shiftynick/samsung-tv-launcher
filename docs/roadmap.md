# Experience improvements under investigation

This document separates research and proposed changes from implemented launcher features. The repository is now named Samsung TV Launcher; the initial dinosaur designs are bundled themes rather than the project's identity.

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

A private read-only S90D service/process/settings inventory has been collected. A DNS and socket baseline followed, then a temporary, reversible override of three ad hostnames; see the [S90D privacy audit](s90d-privacy-audit.md). No decrypted traffic audit or service cleanup has been performed, and no block is persistent.

## Theme packs

Implemented: runtime discovery of data-only theme folders, a paginated picker, palette/art/font/style manifests, bounded validation, empty-catalog recovery and host-side create/clone/validate tools. The four original dinosaur designs remain bundled, with Neon District added as a cyberpunk pack. A plain theme is no longer offered in normal selection; the authoring tool can still generate a minimal starter. See [theme-pack authoring](theme-packs.md).

Authorized host-side installation and reload is the initial workflow. USB import, online downloads, a marketplace and arbitrary code-defined layouts are separate possible features. Theme data remains separate from app IDs, HDMI configuration and device access profiles.
