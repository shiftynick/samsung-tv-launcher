# S90D privacy audit

This page records what was measured on the owner's S90D (firmware 2130.0) in October 2026, a small blocking trial that followed it, and what neither of them shows. Raw captures, addresses, device identifiers and the original system files stay private. The older [privacy scope](08-privacy.md) page describes upstream research on a different TV and is not evidence about this one.

The owner reports that **Viewing Information Services** (Samsung's ACR opt-in) and **Interest-Based Advertisements** were already turned off before the audit. Neither setting was changed, and their state was not read back through a native API.

## 1. Baseline: what the TV looked up

**Method.** The TV's own `tcpdump` recorded DNS questions and answers as decoded text on the Wi-Fi interface (non-promiscuous; no packet files, no TLS or HTTP content). Once a minute, a socket snapshot recorded which process owned each connection. A controlled `example.com` lookup confirmed that the capture also saw queries sent directly to the public resolver the TV keeps as a fallback, which a DNS server on another machine would have missed. Nothing was installed permanently, and the recorder ended itself.

**Coverage.** Two consecutive runs on October 27–28 span about 22 hours, but this is **not a continuous 24-hour record**. Socket samples stop for about 8 hours overnight, possibly because of standby; the cause was not established and the gap must not be read as "no traffic". The second run also stopped earlier than planned for an unknown reason. Both runs reported zero kernel drops; a small counter discrepancy at shutdown is kept as a stated limitation.

**Totals.** 3,919 DNS questions for 45 distinct names, and 828 socket samples. A and AAAA questions count separately. These are lookups, not uploads, ad impressions or viewing events.

| Name (or group) | Questions | Context |
| --- | ---: | --- |
| `connectivitycheck.gstatic.com` | 1,678 | Connectivity checking, as in [Chromium's Cast checker](https://chromium.googlesource.com/chromium/src/+/master/chromecast/net/connectivity_checker_impl.h) |
| `unagi-na.amazon.com` | 572 | Amazon; exact role unverified |
| `nrdp.logs.netflix.com`, `logs.netflix.com` | 429 | Netflix endpoints named for logging; content unknown |
| `api.open-meteo.com` | 36 | This launcher's [weather](reactive-scenes.md) |
| `cdn-stv.adgrx.com` | 14 | AdGear (Samsung Ads) domain; role inferred from its name |
| `tvx.adgrx.com` | 10 | AdGear; its DNS alias pointed at an `rtb-catalyst` name, which suggests ad-bidding infrastructure |
| `rtb-config.samsungads.com` | 2 | Samsung Ads; role inferred from its name |
| `googleads.g.doubleclick.net` | 2 | Google ad requests, per [Google's documentation](https://support.google.com/admanager/answer/4359047) |

Other lookups went to Samsung cloud, image and programme-guide services, and to Netflix and Amazon delivery and push services. Samsung's own [cookie table](https://www.samsung.com/tr/info/privacy/cookie-policy-details/) lists `adgrx.com` as AdGear advertising and targeting; that describes the domain, not these exact endpoints or the purpose of each request.

**What this does not show.** Ad-related lookups continue with both opt-outs set, but the audit does **not** show that ACR is active despite the opt-out, that screen images are uploaded, or what any encrypted connection carried. No TLS content or server names were captured. Encrypted DNS, cached names and non-standard DNS ports are outside its view. Short-lived sockets and shared resolvers mean many lookups cannot be tied to a process; some DNS traffic mapped to Netflix. Activity (idle Home, streaming, standby) was not labelled continuously, so lookups cannot all be attributed to a state.

## 2. Trial: three ad hostnames overridden

The owner then authorised a narrow, reversible trial for exactly three names, added one at a time:

1. `rtb-config.samsungads.com`
2. `tvx.adgrx.com`
3. `cdn-stv.adgrx.com`

**Mechanism.** The TV's resolver reads `/etc/hosts` before DNS. The root filesystem stays read-only: a copy of the original hosts file, with every existing entry kept and the trial names mapped to `0.0.0.0` and `::`, is **bind-mounted** over `/etc/hosts` in the init process's mount namespace. Owner, mode and SMACK label match the original. Because that mount propagates, running apps (the launcher, Samsung Home and Netflix were checked) saw the same file, and fresh lookups returned only the override addresses. Each step replaced the single overlay rather than stacking mounts. The original file was backed up on the TV and the host before any change, and a guarded undo checks for drift, removes only the overlay and verifies the original.

**Checks.** After each addition the owner confirmed Home to Samsung Home and back, streaming with sound, HDMI and settings; after the third, Samsung Home images and menus as well. That covers what was checked, not every app or feature.

**Status.** As of October 28, 2026 all three overrides are **active and temporary**. A reboot removes them: nothing was made persistent and no service was added. A finite follow-up recorder (DNS questions only, plus per-minute socket snapshots) is running for up to 24 hours to look for remaining lookups. Its results have not been reviewed yet; this page will be updated when they are.

**Limits.** This is a system-resolver override, not a firewall, router-wide or DNS-server block. Apps with their own DNS, cached answers and already-open connections can bypass it, and a lookup answered from the hosts file never reaches the network, so the follow-up cannot count blocked attempts. No reduction in data sent has been measured. Blocking the DoubleClick name was not tried and is not a known way to remove YouTube ads. Shared Amazon, Netflix and connectivity services were deliberately left alone.

## 3. Not done

- No Samsung service, package or preload was disabled. The upstream U8000F cleanup profile remains untested here and is not an S90D disable list.
- No consent setting, DNS server, router setting or account was changed.
- No permanent blocking. Making the trial survive reboots would be a separate decision with its own checks.

Source for the opt-out itself: [Samsung's ACR explanation](https://www.samsung.com/us/support/answer/ANS10010616/).
