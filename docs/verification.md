# Verification record

This is a sanitized summary, not a raw device log or a claim of support for every Samsung TV.

Recorded target: `QN77S90DDFXZA`, firmware `T-PTMDAKUC-0090-2130.0`, Tizen 9.0, ARMv7 Linux 5.4.249, .NET 6.0.9.

## Owner checks

The owner confirmed the initial launcher rendered, remote navigation worked, Plex played with sound, the HDMI shortcut switched to the connected input, and Netflix/Apple TV/Prime Video/YouTube were good. After installation of the four themed designs, the owner reviewed the UI and approved switching Home to it. YouTube TV was added later; its playback is not separately confirmed.

## On-device automated checks

- Authenticated temporary UID/EUID 0 and cleanup verification, with passwd/SDB-key hashes unchanged.
- Six installed application IDs found in the TV's registry.
- Four themes changed through app-control requests with no preference-write errors and all 12 retained app-image views ready in each theme.
- Saved Dino Den choice survived a launcher process restart; Cinematic was then selected as the default starting theme.
- Theme picker opened and the process remained running with six catalog entries.
- Home/startup operation and four preference values were changed using native APIs and read back. Existing Home/startup Buxton read/write privileges were compared before/after.

A launcher process restart is not a TV cold boot. The owner confirmed the physical Home → Samsung Home shortcut → Home round trip. The owner also confirmed that the custom launcher opened automatically after a full TV restart. Standby/wake remains a separate check. Software launch/readback checks do not establish playback quality, latency, absence of tracking, or boot behavior.

## Offline checks

Python tests cover explicit identity gates, bounded checksum-verified transfers, synthetic cleanup apply/restore and state drift, routing SQL, recipe syntax, package assets and documentation links. Native catalog tests cover navigation bounds and malformed catalogs. Theme preference tests cover every theme, missing/corrupt state, atomic-save cleanup and refusal to overwrite a valid choice with an invalid value. The NUI launcher and helpers were compiled against private matching device references.

No S90D privacy cleanup, decrypted network audit, controlled latency benchmark or hardware-level recovery was performed. The upstream U8000F observation file remains scoped to that other device.
