# S90D setup and deployment

The recorded target is the exact model/firmware in [the S90D profile](../profiles/s90d-2130.0.json). Other firmware, runtime versions, source IDs and containers need a separate investigation. Read [recovery](09-recovery.md) first. The source remains a case study with a reusable toolkit, not a one-click installer.

## Host and access

Install Python 3.12+, OpenSSL, an SDK capable of building net6.0, and Samsung's SDB separately. Enable Developer Mode and authorize the controller's LAN IPv4 on the TV. Start with the [README commands](../README.md#get-started). All examples use reserved documentation addresses; replace them with your own device's values privately.

For convenience, define a shell helper in your own terminal, not in a public script:

```sh
tv() { python3 tools/tv.py --host "$TV_IP" --firmware "$TV_FIRMWARE" --sdb "$SDB" "$@"; }
python3 tools/build.py payloads
tv doctor
tv root-check
```

`root-check` is an actual temporary root acquisition, not a read-only compatibility test. Keep TV sessions sequential. A normal retail `sdb shell` closing is expected; the archive route uses a different path. Root acquisition is temporary, authenticated, and does not install boot persistence. Do not substitute the upstream QN90B/QN90F kernel exploits.

Inspect your TV's runtime, registry schema and installed app IDs before deployment. Preserve the six selected apps, their account data, HDMI/display/OLED care, audio, networking, Bluetooth, accessibility and native settings. The included U8000F cleanup profile is legacy research; it is not validated for this S90D.

## Framework references and build

Find these files on your own TV: Tizen.NUI.dll, Tizen.Applications.Common.dll, Tizen.System.dll and Tizen.dll. The observed directory was `/usr/share/dotnet.tizen/framework/`.

```sh
tv pull /usr/share/dotnet.tizen/framework/Tizen.NUI.dll .local/tv-framework/Tizen.NUI.dll
# Repeat for the other three files after confirming their actual paths.
python3 tools/build.py launcher --framework .local/tv-framework
```

Transfers are checksum-verified and bounded. A file above the transfer limit must be split and verified separately, not accepted truncated. Never commit the framework files, Samsung executables or resulting DLLs. The reference entries use Private=false so Samsung assemblies are not packaged into the launcher.

Build outputs include the launcher, optional settings bridge, Probe and HomeRoute, plus a `quiet-home/` asset tree. The existing package/assembly identity and resource directory are retained for compatibility with the tested container.

## Back up actual state

Before the first install, make a fresh private snapshot using `recipes/snapshot.sh`, retain the original package including metadata/native-image cache, and save the actual Home operation, startup selectors, owners and SMACK labels. Keep copies on TV and computer. Use SQLite `.backup` and integrity checks; do not copy a live WAL database as if it were a consistent snapshot. Generate inverse operations before mutation. Never overwrite the first backup after a partial failure.

Use Probe to inspect the source list and current Home handler. Verify the app IDs in `apps.json` against the TV. The example HDMI-3 source type is 15; do not infer another TV's mapping or cable arrangement from that number.

```sh
tv push .local/package/Probe.dll Probe.dll
tv exec --command '/usr/bin/dotnet /home/owner/share/tmp/sdk_tools/quiet-tizen/Probe.dll'
```

Inspect the `com.samsung.tv.bixby-promotion` package before replacing anything. The observed entry assembly owner was 201:0. If the owner, labels, package layout, runtime or schema differ, stop and adapt a separate profile. Preserve original application files and the framework integration; this is not a signed standalone app installation.

## Install and review first

The [launcher recipe](../recipes/install-launcher.sh) is inherited from the upstream case study and requires inspection/adaptation of the inventoried container before use. It backs up the package, replaces the entry assembly/resources, and launches the UI without changing Home. It is phased filesystem work, not an atomic installer. Inspect what completed if a step fails; do not rerun by deleting backups.

Build from source, package the local output, upload it to the explicitly selected TV, and apply ownership/permissions/SMACK labels matching the original package. The root agent uploads scripts privately and checks cleanup. Review six app tiles, physical D-pad/Select/Back behavior, each streaming app and sound, HDMI/input switching, native settings, bundled themes, picker pagination, and theme persistence. Backgrounds must have no decorative text; labels remain native UI. Three redundant top-right shortcuts have been removed.

See [theme packs](theme-packs.md) for data-only authoring and runtime discovery. Theme resources live under `quiet-home/app-icons/themes/`. The selection file is `launcher-theme.txt` in the app's own data directory. Switching themes does not alter Samsung system preferences or app accounts. Pixel mode uses a licensed pixel font and nearest-neighbor image sampling; other themes use the bundled Quiet Inter fonts.

Only after the owner has reviewed the visible UI, follow [Home routing](11-home-routing.md). Keep the built-in Samsung Home and full-settings shortcuts working. Test full restart and standby/wake separately after temporary root cleanup ends. Do not claim full-boot behavior based only on restarting the launcher process.

## Maintenance

Keep raw sessions, transfer scripts, model-specific private config, artwork receipts, screenshots and backups under `.local/`. The source audit intentionally excludes that directory. Re-check routing and compatibility after Samsung updates. Do not silently disable firmware updates or add an enforcement daemon.
