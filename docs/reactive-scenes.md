# Reactive scenes

Home follows the time of day, the season and, once you choose a place, the local weather. The effect is a native overlay drawn above the theme's own scene layers and below the tiles, so it works with every theme pack without new artwork.

| Input | What changes |
| --- | --- |
| Time of day | Dawn and dusk add a warm tint; night adds a deep blue tint and, under a clear sky, a few twinkling stars. With a place set, the phase follows the sun's elevation there (day above 8°, night below −8°); without one, the TV clock decides. |
| Weather | Clouds and fog add a grey cast and slow drifting bands; rain and storms add falling streaks; snow adds drifting flakes. Lightning flashes appear only with Motion set to Full. |
| Season | Falling leaves in autumn and petals in spring during daylight; fireflies on summer evenings. Southern-hemisphere places get the opposite season. |

Motion Off keeps the colour tint and removes all movement. Effects run only on Home in the foreground and pause with the rest of the motion. The weather reading appears beside the clock.

## Setting it up

Use the **Scenes** section of the [phone companion](companion.md):

- Turn scenes on or off.
- Search for a city or postcode and pick a match. This turns weather on.
- Choose °F or °C.
- Preview any time, weather and season combination on the TV. A preview lasts one minute; the TV returns to the current conditions within 30 seconds after that.

## Network and privacy

Weather comes from [Open-Meteo](https://open-meteo.com/), which needs no account or key. The TV requests the current temperature and weather code every 30 minutes, only while the launcher is in the foreground and weather is on. Requests carry only coordinates rounded to two decimals (about a kilometre); place searches send the text you type. The place and the last reading are stored only in the launcher's data directory (`launcher-sky.json`, `launcher-weather.json`). A reading older than three hours is ignored and the scene falls back to clear.

The launcher's package does not declare an internet privilege, but on the recorded S90D firmware its process can make outbound HTTPS requests. If a firmware update blocks that, scenes keep working from the clock and calendar and the weather line stays empty.
