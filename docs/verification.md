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

The manifest-based update was also checked on this TV: all five bundled packs loaded without catalog/preference errors; the four artwork packs each reached 12/12 retained image views, and Neutral used no artwork views. A temporary sixth data-only pack was discovered, selected and removed while the same launcher process remained running, without rebuilding or restarting it. The picker reached its second page, the original Dino Den choice survived a new launcher process, and native routing readback remained unchanged. The owner subsequently confirmed that all five themes and picker pagination looked and navigated correctly. That confirmation predates the later motion/cyberpunk update.

The motion/cyberpunk update was deployed with a fresh TV/host rollback checkpoint. The four dinosaur packs and Neon District each loaded with 12/12 retained artwork views and no reported theme/motion errors. Off mode retained zero continuous animations; Subtle created six particle loops; Full created the declared twelve for Neon District. Pixel art opted out of selected-icon float. The theme picker cleared continuous animations. Automated app-switch checks observed the paused state behind Samsung Home and resumed state on return; the saved cyberpunk theme and Full preference survived a new launcher process. Subtle was then selected for review. Native routing readback remained unchanged. These checks establish state/readback and successful native API calls, not couch-distance appearance, GPU timing, streaming playback after this update or TV cold-boot behavior of the motion feature.

The living-scene update adds a separately backed-up Neon District plate, three moving hovercar views, two mist views and a selected Netflix blink frame. On the recorded TV, all five scene images and the alternate tile frame reached Ready with no reported motion error. Off removed the scene/frame; the picker cleared them; all four original packs still loaded with 12/12 artwork views. The app-switch check observed paused scene state and alternate-frame release behind Samsung Home, then ready scene/frame on return. A new launcher process retained the selected theme/motion setting; routing readback remained unchanged. The owner confirmed that the revised scene/character motion matches the desired direction. GPU/FPS measurements remain separate.

The local photo-screen update was backed up on TV/host and installed with a private owner-selected album. On-device checks loaded the album and first image, advanced repeatedly through both reusable crossfade views, changed album filters, verified instant transitions with Motion Off, observed automatic slideshow advancement, stopped the timer behind Samsung Home and returned to Home. The private album loaded again after a new launcher process; routing readback stayed unchanged. Host tests checked safe paths/JPEG bounds/navigation, source preservation and metadata removal. Personal photos, source selections, travel labels and catalog/provenance remain excluded from this repository. The owner subsequently confirmed that the photo choices, display and remote controls looked good.

The four dinosaur packs now use separately generated scene/tile assets, preserving every original wallpaper and icon. Automated TV checks reached Ready for all declared scene views and each selected alternate frame: Cinematic four layers/Apple TV blink, 8-bit Jungle three layers/Prime Video blink, Fossil Club one light layer/Prime Video pulse, and Dino Den two layers/Netflix blink. Neon District's five scene layers and selected frame also remained Ready. Generic particles were disabled for all five packs. The renderer adds nearest-neighbor sampling for pixel scene/frame images and validated blink/pulse/flutter tile kinds. Off and picker cleanup passed. A full Plex app switch paused motion/released the Fossil alternate frame, and Home return restored it. Photos still loaded the private album. A direct external Samsung Home request did not reliably produce a pause transition in this run; launcher-owned app shortcuts explicitly stop motion before launch. Cinematic scene loading and its selected frame also survived a new launcher process. Native routing readback and checksums of the complete private photo album matched the pre-update snapshot. These checks establish successful loading/API calls; the owner subsequently confirmed that all four revised packs looked good. GPU timing remains separate.

A launcher process restart is not a TV cold boot. The owner confirmed the physical Home → Samsung Home shortcut → Home round trip. The owner also confirmed that the custom launcher opened automatically after a full TV restart. Standby/wake remains a separate check. Software launch/readback checks do not establish playback quality, latency, absence of tracking, or boot behavior.

## Offline checks

Python tests cover explicit identity gates, bounded checksum-verified transfers, synthetic cleanup apply/restore and state drift, routing SQL, recipe syntax, package assets and documentation links. Native catalog tests cover navigation bounds and malformed catalogs. Scene tests reject unsafe/missing assets, unknown nested fields, excessive layers/area and invalid animation bounds; the authoring CLI cloned and revalidated scene assets. Theme tests cover dynamic discovery, invalid manifests/assets and unsafe paths, empty-catalog recovery, pagination bounds, missing/corrupt preferences, atomic-save cleanup and refusal to overwrite a valid choice with an invalid value. The NUI launcher and helpers were compiled against private matching device references.

No S90D privacy cleanup, decrypted network audit, controlled latency benchmark or hardware-level recovery was performed. The upstream U8000F observation file remains scoped to that other device.
