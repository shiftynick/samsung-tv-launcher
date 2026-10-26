# Working on Samsung TV Launcher

This is a model-specific native Tizen launcher derived from Quiet Tizen. Read README.md, docs/10-s90d-setup.md, docs/11-home-routing.md, docs/09-recovery.md and docs/verification.md before any TV work.

Only the S90D model/firmware in profiles/s90d-2130.0.json is recorded for this launcher. Firmware must be read on the owner's TV; --firmware is an operator declaration. Use the owner's explicit TV address. Do not scan the network or widen a model gate to make another TV pass. Inventory actual app IDs, source IDs, runtime, owners, SMACK labels and routing before mutation.

Preserve display/OLED care, HDMI, audio, networking, Bluetooth, accessibility, package management, chosen apps, logins and app data. Do not flash firmware, replace a bootloader, modify passwd/SDB keys, globally disable org.tizen.menu, or introduce an always-on root daemon. A privacy feature name is not evidence of exfiltration. No S90D cleanup profile has been validated; the U8000F profile and older cleanup docs are upstream research, not an S90D disable list.

Work in phases: private inventory; consistent SQLite snapshots and original package/metadata backups; locally built payloads; manual launcher installation; visible remote/apps/HDMI/sound/settings checks; Home/startup routing; separate full-boot and standby/wake checks. Save original backups on both TV and host before changing files or values. Prepare inverse operations first. Never overwrite the first backup. Do not restore whole databases over live state; restore specific values through native APIs with drift guards. Use HomeRoute for routing values so Buxton privilege labels are preserved.

Run TV/root sessions sequentially, with commands below the 30-second budget. Let cleanup complete before restarting or powering down. Do not reboot or enter standby inside a temporary root session. After a failure inspect actual state before retrying; a later failure does not imply earlier steps were undone.

Personal device profiles, raw command output, TV screenshots, captured traffic, exported databases, framework/SDK files, compiled outputs and rollback archives stay under ignored .local/. Downloaded publisher art stays under ignored app-icons children; only the generated themes subtree is publishable. Do not upload original Samsung binaries, firmware, serial/MAC/DUID/account identifiers, credentials, real network addresses or personal filesystem paths. Public example addresses use reserved documentation ranges only. Audit the staged file list and image metadata, not just working-tree text, before every public commit. Do not commit local history or an entire working directory.

Preserve upstream notices and separate font/icon licenses. Theme artwork is generated; keep app names and navigation as native UI, not text baked into wallpapers. Keep the theme cache bounded to the active theme and preserve its preference in the launcher's own data directory. Test preference persistence, malformed values, catalog navigation and relevant rollback logic. Build NUI changes against matching privately obtained references and report hardware-test limits accurately.

Theme manifests use the shared ThemePack parser on host and TV. Validate bundled and contributed packs with tools/ThemeTool; test dynamic discovery, pagination, invalid packs, first-valid/Cinematic fallback and empty-catalog recovery. Keep new themes data-only and do not add theme-ID branches to Home.cs.

Motion effects must be bounded native animations, foreground/Home-only, paused before launching other apps, and disposed before destroying their views. Test saved Off/Subtle/Full behavior; preserve remote navigation and app/HDMI playback.
