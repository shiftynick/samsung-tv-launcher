# Phone companion

The launcher can serve a small web page to phones on your home network. From it you can switch themes and motion, reorder or hide Home tiles, rename the HDMI shortcut and choose its input, and add or delete photos in the private album. Nothing is installed on the phone.

## Use

1. On the TV, select **Phone** in the bottom row of Home and turn **Phone access** on.
2. Scan the QR code with the phone camera, or open the address shown (for example `http://192.0.2.50:47801`) and type the six-digit code.
3. The phone keeps a token in its browser storage. Later visits open straight to the controls.

**Forget phones** on the TV revokes every phone; **Unpair this phone** on the page revokes only that one. Turning phone access off stops the server.

## How it works

- The server runs inside the launcher process (`HttpListener`, TCP 47801) with the launcher's own user and security label. There is no root service and nothing runs when the launcher is closed.
- A pairing code exists only while the Phone screen is open, works once, and is cancelled after five wrong guesses. The QR code carries the same code in the URL fragment, which the browser does not send to the TV; the page pairs automatically after a scan.
- Tokens are 256-bit random values. The TV stores only their SHA-256 hashes, for at most eight phones, in the launcher's data directory.
- Requests are refused unless they come from a private, loopback or link-local address and address the TV by IP. Refusing host names blocks DNS-rebinding pages from reaching the API.
- The page uses no cookies, loads no outside resources and is served with a restrictive Content-Security-Policy.
- Layout changes are validated against the bundled app catalog and saved to `launcher-layout.json`. Photo changes are written beside the album, re-read through the same validator as the photo screen, and rolled back if that fails.
- Photos are resized on the phone to at most 1920×1080. Canvas re-encoding drops EXIF data, including location. Portrait photos are centred on a blurred landscape background because the TV album accepts landscape images only.

## Limits

- Traffic is plain HTTP on the local network. Anyone who can watch your Wi-Fi traffic could copy a token; use it on a network you trust.
- HDMI 1 and 2 use source IDs 13 and 14 by analogy with the verified HDMI 3 (15); only HDMI 3 has been confirmed on the recorded TV.
- If the TV freezes the launcher while another app is in the foreground, the page may not respond until you return to Home.
