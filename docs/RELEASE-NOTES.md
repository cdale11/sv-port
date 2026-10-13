# Stardew Valley for RG34XXSP / KNULLI — v1.0.0

A stable ARM64 PortMaster release for Anbernic RG34XXSP running 64-bit KNULLI,
with Windows/Linux Steam mainline import handling, internal rumble and optional
SMAPI support. Supply your own Stardew Valley installation; no retail files are
included.

## Included features

- Built-in RG34XXSP motor rumble with an independent shutdown/watchdog helper.
- KNULLI-selected battery/performance HUD preserved by default.
- Preparation caching for unchanged game files and dependencies.
- All available CPU cores eligible for background work, reversible performance
  governor selection, workstation GC and tiered JIT/PGO.
- Reduced texture-upload synchronization and larger initial sprite pools.
- Persistent saves, PortMaster controller/display integration, startup diagnostics
  and optional SMAPI 4.5.2.

## Install or upgrade

**New installation:** extract `stardewvalleymainline.zip` into
`/userdata/roms/ports/`, then copy a complete owned Windows or Linux Steam .NET 6
mainline installation into `stardewvalleymainline/gamedata/`.

**Existing v0.1.x installation:** close the game, back up saves and extract
`stardewvalley-rg34xxsp-performance-update.zip` into `/userdata/roms/ports/`,
allowing replacement. Saves, mods, retail game files and configuration are
preserved. Runtime behavior is unchanged from v0.1.3; there is no required
configuration change. The full install package contains the refreshed docs and
metadata; the upgrade contains only runtime components and helpers.

## Compatibility and operation

Target: RG34XXSP, 64-bit KNULLI, Stardew 1.6 mainline .NET 6 layout. Reference
configuration: KNULLI Scarab, game assembly 1.6.15.24356. Game startup can take
tens of seconds even when preparation is cached. Main simulation/rendering
remains single-threaded; performance varies with farm complexity and mods.
Performance governor mode can increase heat and battery usage.

Use the in-game quit option for normal shutdown. Fn+Start does not save progress
and can produce native exit status 135/139. Steam services are unavailable;
legacy compatibility-branch game files are unsupported. Firmware-dependent
features, future game updates and individual mods have separate compatibility
requirements described in the guide.

## Release checks

Automated checks cover launcher/CPU/HUD policy, Windows/Linux import fixtures,
managed/native rumble routing, package integrity and .NET 6.0.36 SDL/OpenGL
rendering/audio behavior. Graphics checks include full/partial texture ordering,
8192 sprites, sprite-pool growth and audio-worker shutdown in both texture
synchronization modes. These checks do not guarantee performance or every
firmware, retail-version and mod combination.

[Installation and configuration](https://github.com/cdale11/sv-port#readme) ·
[Performance settings](https://github.com/cdale11/sv-port/blob/main/docs/PERFORMANCE.md) ·
[Quality assurance and compatibility](https://github.com/cdale11/sv-port/blob/main/docs/HARDWARE-VALIDATION.md)
