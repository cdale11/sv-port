# Stardew Valley — RG34XXSP / KNULLI

ARM64 adaptation of the Stardew Valley mainline PortMaster port for
Anbernic RG34XXSP running 64-bit KNULLI. Includes internal motor rumble, preparation
caching, KNULLI battery HUD, CPU/rendering optimizations and optional SMAPI. Stardew Valley is sold separately.

## Installation

Install/update PortMaster, then copy a complete clean Windows or Linux Steam
.NET 6 mainline game installation into `ports/stardewvalleymainline/gamedata/`.
Disable the legacy compatibility beta. Game files from different platforms or
versions must not be mixed. The copied files are prepared on first launch.

Leave `stardewvalleymainline/Mods/` empty for vanilla play, or install compatible
SMAPI mods there. Persistent saves live in `stardewvalleymainline/savedata/`.
Enable vibration in the game and leave `SDV_RUMBLE=1` in `rg34xxsp.conf` for the
internal motor.

## Documentation and limits

The [repository README](https://github.com/cdale11/sv-port#readme) contains downloads,
upgrade steps, controls, settings, troubleshooting and compatibility requirements.
The target is RG34XXSP with 64-bit KNULLI and the Stardew 1.6 mainline .NET 6
layout. Startup can take tens of seconds. Firmware features and mods depend on
configuration and compatible dependencies. Use the in-game quit option for normal
shutdown; Fn+Start does not save progress and can produce a native exit error.
Steam overlay, achievements, cloud saves and Steam-invite multiplayer are unavailable.

Based on Producdevity’s mainline port and Johnny on Flame’s original PortMaster
work. See [credits](https://github.com/cdale11/sv-port/blob/main/docs/CREDITS.md) for
upstream sources, the RG34XXSP adaptation and AI-assisted development disclosure.
