# Stardew Valley — RG34XXSP / KNULLI

Experimental ARM64 adaptation of the Stardew Valley mainline PortMaster port for
Anbernic RG34XXSP running 64-bit KNULLI. Includes internal motor rumble, preparation
caching and optional SMAPI. Stardew Valley is sold separately.

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
upgrade steps, controls, settings, troubleshooting and current validation limits.
Gameplay and internal rumble have been reported working; performance comparisons,
full retail-version coverage and suspend/resume still need hardware validation.
Steam overlay, achievements, cloud saves and Steam-invite multiplayer are unavailable.

Based on Producdevity’s mainline port and Johnny on Flame’s original PortMaster
work. See [credits](https://github.com/cdale11/sv-port/blob/main/docs/CREDITS.md) for
upstream sources, the RG34XXSP adaptation and AI-assisted development disclosure.
