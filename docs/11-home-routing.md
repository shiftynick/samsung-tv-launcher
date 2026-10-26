# Home button and startup routing

The supported sequence is: verify the installed launcher; record actual original settings and privileges; back them up locally and on TV; prepare rollback; change selected values through native APIs; verify readback and unchanged privileges; test the physical remote, full boot and standby/wake separately.

`HomeRoute.dll` is built from [HomeRoute.cs](../launcher/probe/HomeRoute.cs). It changes the main Home operation and only these preferences:

- `db/appfw/home_appid`
- `db/appfw/startup_appid`
- `db/appfw/booting_appid_backup`
- `db/menu/smart_hub/ontvsetting/auto_start`

It uses `app_operation_set_current_app`, `vconf_set_str` and `vconf_set_int`, with readback. It does not use `vconftool set` to replace key metadata. Existing Buxton read/write privilege labels must be recorded and compared before/after. If state differs from the saved snapshot, the helper refuses to overwrite it. A failed change attempts to restore the captured previous state.

## Capture and prepare recovery

Upload your locally built HomeRoute helper, then copy it into a new private backup directory on the TV. Choose a phase name that does not already exist; never overwrite an original snapshot. Save consistent backups of the inventoried owner app-service and system preference databases for recovery reference; restore values with native APIs rather than copying entire databases over live state.

Using a freshly prepared directory, for example `/opt/usr/share/quiet-tizen-backups/routing-01`:

```sh
# These are individual commands run through your explicit-target tv helper.
tv push .local/package/HomeRoute.dll HomeRoute.dll
tv exec --command '/usr/bin/dotnet /home/owner/share/tmp/sdk_tools/quiet-tizen/HomeRoute.dll query'
# Create the phase directory and save the helper/recovery files first.
tv exec --command '/usr/bin/dotnet /opt/usr/share/quiet-tizen-backups/routing-01/HomeRoute.dll snapshot /opt/usr/share/quiet-tizen-backups/routing-01/original-routing.json'
```

Snapshot creation uses CreateNew and refuses an existing file. Store the original JSON and database/metadata backups on both computer and TV before applying. The snapshot contains actual original app IDs and autostart value; do not assume someone else's defaults.

The recorded S90D had the original app ID `com.samsung.tv.csfs`. The custom container is `com.samsung.tv.bixby-promotion`. Query and verify your own target.

## Apply and verify

```sh
tv exec --command '/usr/bin/dotnet /opt/usr/share/quiet-tizen-backups/routing-01/HomeRoute.dll apply /opt/usr/share/quiet-tizen-backups/routing-01/original-routing.json com.samsung.tv.bixby-promotion'
```

The operation and all four keys must read back as the intended custom values (autostart 1). Compare the captured read/write privileges with their original values; changing the launcher is not authorization to remove access controls. On the recorded firmware, `buxton2ctl get-read-priv system KEY` and `get-write-priv system KEY` expose the relevant key privileges. Use available checksum tools if `cmp` is absent.

After root cleanup, press Home. It should open the custom launcher. Select its Samsung Home shortcut, then press Home again: this should return to the custom launcher. This physical round trip was confirmed by the owner. The Samsung Home shortcut directly launches the stock app; it does not reset the default assignment.

The owner confirmed that the custom launcher opened automatically after a full TV restart on the recorded S90D. Full TV restart and standby/wake are separate checks. Let any temporary root session finish before either. Preference readback alone does not prove startup behavior on another firmware.

## Undo routing

Keep this inverse command in the same private backup directory and a local copy:

```sh
tv exec --command '/usr/bin/dotnet /opt/usr/share/quiet-tizen-backups/routing-01/HomeRoute.dll restore /opt/usr/share/quiet-tizen-backups/routing-01/original-routing.json com.samsung.tv.bixby-promotion'
```

It restores the original operation, Home, startup, boot-backup and autostart values with drift guards. If a firmware update changed them independently, inspect before reconciling; do not remove the guards. Restoring Home is separate from restoring the original Bixby Promotion assembly or undoing a selected privacy cleanup group.
