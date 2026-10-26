# Recovery and rollback

Keep Developer Mode/controller authorization and private original backups available until full boot, standby/wake, Home, native settings, apps, HDMI and sound have been checked. Do not factory-reset as a first response: a reset can remove useful state without restoring modified package files.

## Restore original Home routing

Use the recorded `original-routing.json` and the HomeRoute helper saved with that phase. Follow [the native restore procedure](11-home-routing.md#undo-routing). It restores the actual saved operation, Home, startup, boot-backup and autostart values and refuses unexpected state drift. Do not infer original values from another device. Do not use older `vconftool set` examples to replace key metadata.

Routing restoration is separate from restoring a package's assembly/resources. Returning the Home button to Samsung does not require turning every optional background feature back on.

## Restore the previous launcher build

Stop only the custom launcher, verify the currently installed assembly matches the phase's recorded checksum, and restore the saved assembly/catalog/native-image cache with their original owner, modes and SMACK labels. Retain the first original Samsung package backup as well as later custom-build checkpoints. Extra theme resources can remain inert until recovery is verified; do not broad-delete unrelated directories.

The included upstream `restore-launcher.sh` predates the S90D HomeRoute workflow. Treat it as a reference for package/registry restoration, not the current S90D routing command. Inspect and adapt it to your recorded state; restore routing through HomeRoute first. Original package/app metadata and any selected flag changes must be restored field-by-field, not by copying an old database over the live registry.

## After a failed command

Inspect which operations completed before retrying. Filesystem recipes are phased, not one transaction. Do not delete or overwrite a snapshot to make a retry succeed. Preserve the root session log privately. A successful root session also needs successful staging cleanup and unchanged system-file hashes. Let cleanup finish before any restart or power transition.

If the app is blank, inspect the current state file, resource paths, owners/labels, matching framework, image-decode state and native-image cache. If Home opens the wrong app, query the operation and all four preferences rather than changing unrelated services. If firmware changed, re-establish exact compatibility before trying another access route. No hardware unbrick procedure is provided.

## Optional upstream cleanup and settings bridge

No S90D cleanup or sidebar/settings-bridge modification was applied in this project. If you independently use the upstream recipes, retain the specific generated inverse SQL, service-marker states and original preferences for each phase. Drift guards must remain in place. Runtime masks disappear after restart; one absent process is not evidence of permanent removal. Keep native advanced/network/Bluetooth settings and OLED care available.
