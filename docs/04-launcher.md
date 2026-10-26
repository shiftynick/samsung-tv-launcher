# Launcher build and installation

The current dinosaur launcher workflow is documented in [S90D setup](10-s90d-setup.md), [Home routing](11-home-routing.md), [verification](verification.md) and [recovery](09-recovery.md).

This is native .NET/NUI code running in the existing Bixby Promotion container. It does not install Android software or replace Tizen's display/audio/media stack. Four sets of generated PNG art are included, while app names, clock, theme choice, focus and navigation are independent native controls.

Build with `python3 tools/build.py launcher --framework .local/tv-framework` after obtaining the four matching Tizen framework assemblies privately. `Home.cs` implements UI/navigation; `ThemePreference.cs` stores a validated local preference with atomic replacement. `apps.json` provides six app IDs and theme-art keys. Runtime source IDs, app/container paths and owners are observations for the recorded S90D, not universal Samsung constants.

Back up the original package and routing before mutation. Review a manually launched UI before assigning Home. Use HomeRoute's native value setters and saved state for routing; older upstream CLI preference-setting examples are not the current S90D procedure. Do not globally disable Samsung's menu or remove OLED care, sound, HDMI, Bluetooth, accessibility, app frameworks or networking.

The optional QuickLaunch bridge and upstream cleanup recipes remain source references; they were not applied as part of the S90D theme/Home setup. Automatic root reacquisition, a TV-side watchdog and permanent root are not installed.
