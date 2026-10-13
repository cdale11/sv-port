# Stardew Valley for RG34XXSP / KNULLI

[![Port checks](https://github.com/cdale11/sv-port/actions/workflows/checks.yml/badge.svg)](https://github.com/cdale11/sv-port/actions/workflows/checks.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

An ARM64 PortMaster fork of [Producdevity’s Stardew Valley mainline port](https://github.com/Producdevity/portmaster-stardew-valley-mainline), adapted for the **Anbernic RG34XXSP running KNULLI**. It adds built-in motor rumble, Windows/Linux mainline import handling, startup preparation caching, CPU/rendering optimizations and device-specific defaults while retaining optional SMAPI support.

Stardew Valley is sold separately. **No retail game files are included.**

## Downloads

Current prerelease: **[v0.1.3](https://github.com/cdale11/sv-port/releases/tag/rg34xxsp-knulli-v0.1.3)**.

| Your installation | Download |
| --- | --- |
| New installation | [Full install ZIP](https://github.com/cdale11/sv-port/releases/download/rg34xxsp-knulli-v0.1.3/stardewvalleymainline.zip) |
| Existing v0.1.0–v0.1.2 installation | [Performance upgrade ZIP](https://github.com/cdale11/sv-port/releases/download/rg34xxsp-knulli-v0.1.3/stardewvalley-rg34xxsp-performance-update.zip) |

Checksums are available on the release page. GitHub’s **Source code** archives contain the build sources and are not complete install packages. See the [changelog](CHANGELOG.md) for previous releases.

## Requirements and compatibility

- Anbernic RG34XXSP with **64-bit KNULLI** and an installed, updated PortMaster.
- An owned, complete **Windows or Linux Steam mainline** Stardew Valley installation using the .NET 6 layout. Leave Steam’s legacy `compatibility` beta disabled.
- Enough SD-card space for the port, copied game files, saves and optional mods.

| Area | Current status |
| --- | --- |
| Gameplay and internal rumble | Reported working by the device owner; v0.1.3 logs confirm the motor bridge initializes on KNULLI Scarab. |
| Windows/Linux imports | Import fixtures pass. Exact retail version/source coverage is still incomplete; future game updates may require port changes. |
| Battery HUD and CPU eligibility | Battery display confirmed working on v0.1.3; the device log shows four CPUs and affinity `0–3`. |
| Startup and large-farm performance | Repeat preparation is 0 seconds; about 32 seconds remain before initialization. Large-farm FPS gains are unmeasured. |
| SMAPI | Bundled version 4.5.2. Individual mods depend on matching game/API versions, available memory and compatible native libraries. |
| Audio recovery, lid suspend/resume, HDMI, external controllers and LAN multiplayer | Firmware integration is retained; full hardware validation is pending. Closing the lid does not save the game. |
| Steam services | Steam overlay, achievements, cloud saves and Steam-invite multiplayer are unavailable through the upstream API stubs. |

This remains an **experimental prerelease**. The importer retains upstream’s Stardew 1.6 patches and checks for the .NET 6 runtime layout. The legacy compatibility branch is unsupported; GOG and other devices/firmware are unvalidated. See [validation details](docs/HARDWARE-VALIDATION.md).

## Install

1. Extract the **full install ZIP** into `/userdata/roms/ports/` on KNULLI. Through its network share, this is `roms/ports/`.
2. Copy the entire clean Windows **or** Linux mainline game installation into `stardewvalleymainline/gamedata/`. Do not mix files from different platforms or versions.
3. Confirm the resulting layout:

   ```text
   /userdata/roms/ports/
   ├── StardewValleyMainline.sh
   └── stardewvalleymainline/
       ├── gamedata/
       │   ├── Stardew Valley.dll
       │   ├── Stardew Valley.deps.json
       │   ├── Stardew Valley.runtimeconfig.json
       │   └── Content/
       ├── Mods/
       ├── savedata/
       └── rg34xxsp.conf
   ```

4. Refresh KNULLI’s game list and launch `StardewValleyMainline` from **Ports**.
5. Enable vibration in Stardew’s settings. Leave `Mods/` empty for vanilla play.

The port modifies the **copied** game files during preparation. Keep the original PC installation as your source. The first launch prepares files and creates a cache, so judge repeat-launch speed on the second launch.

## Upgrade

Close the game, back up `savedata/`, `Mods/` and `rg34xxsp.conf`, then extract the **performance upgrade ZIP** into `/userdata/roms/ports/`, allowing replacement.

The upgrade replaces the launcher, preparation/CPU helpers, patcher, patch DLL and bundled MonoGame overlay. It preserves your game files, saves, mods and configuration, and includes the earlier DLL-lock fix. New settings have defaults even if your existing config lacks them.

This fork uses the upstream `stardewvalleymainline` directory. Installing it over another mainline port updates that installation. When changing the game version or switching Windows/Linux sources, replace `gamedata/` with a clean copy while keeping saves and mods separate.

## What this fork adds

- **RG34XXSP rumble:** MonoGame vibration calls reach KNULLI’s H700 motor interface. A separate helper turns the motor off on pipe closure, termination or a 250 ms command timeout. The internal motor is binary on/off; external player indices retain SDL haptics.
- **Import handling:** Windows/Linux runtime-pack cleanup, removal of conflicting desktop native libraries and managed architecture normalization. Cecil dependency readers now close before overlay replacement, fixing the v0.1.0 locked-DLL abort.
- **Faster repeat preparation:** metadata checks skip launching the patcher for unchanged dependencies. Game, native-library, patcher, overlay and relevant SMAPI changes trigger preparation again. Failures fall back to the normal path.
- **Lower preparation overhead:** architecture checks read DLL headers rather than entire assemblies. Unused mods are not normalized in vanilla mode; SMAPI defaults are copied only during preparation.
- **Device defaults:** 720×480 fallback, PortMaster display dimensions for HDMI, workstation GC, tiered compilation and ReadyToRun. KNULLI-selected battery HUD is preserved. No system overclock is applied.
- **CPU and rendering:** request all online cores, temporarily select the supported performance governor, enable tiered JIT/PGO, remove redundant texture-upload GPU waits and preallocate larger sprite batches. The main game simulation remains single-threaded; see [performance settings and limits](docs/PERFORMANCE.md).
- **Audio shutdown:** stop audio workers before destroying native resources. This addresses a lifetime race. Fn+Start exit can still log a native error; see troubleshooting.
- **Maintenance:** ARM64 cross builds, regression fixtures, package checksums, startup timings, diagnostics, GitHub checks and downloadable upgrade packages.

The base rendering/audio patches, GL4ES integration, Steam API stubs and original SMAPI packaging come from upstream. See [credits and provenance](docs/CREDITS.md) for ownership and contribution details.

## Controls

The port uses KNULLI/PortMaster’s SDL controller mapping. These are the inherited default actions; physical face-button labels follow your controller configuration.

| Input | Action |
| --- | --- |
| Left stick / D-pad | Move or navigate menus |
| Right stick | Move cursor |
| A / B | Confirm or interact / cancel or back |
| X / Y | Use tool / open menu |
| L1 / R1 | Cycle toolbar row |
| L2 / R2 | Cycle toolbar item |
| Start | Pause or open menu |
| Select | Journal |

Use Stardew’s in-game quit option for a normal shutdown. **Fn+Start** is the PortMaster exit shortcut and does not save progress; save at the end of the day before exiting.

## Configuration

Edit `stardewvalleymainline/rg34xxsp.conf` with the game closed.

| Setting | Default | Purpose |
| --- | --- | --- |
| `SDV_LAUNCH_MODE` | `auto` | Vanilla with no user mods; SMAPI when user mods are installed. Set `vanilla` or `smapi` to force a mode. |
| `SDV_RUMBLE` | `1` | Enable the internal motor bridge. `0` leaves SDL controller rumble available. The game’s vibration option also applies. |
| `SDV_RUMBLE_PLAYER` | `0` | Zero-based controller index routed to the internal motor. |
| `SDV_HUD` | `auto` | Preserve KNULLI’s selected battery/performance HUD. `on` enables injection; `off` disables it. |
| `SDV_CPU_AFFINITY` | `all` | Request all online cores; `inherit` retains firmware affinity. |
| `SDV_CPU_GOVERNOR` | `performance` | Temporarily select the supported governor; `inherit` keeps firmware policy. |
| `SDV_TIERED_PGO` | `1` | Optimize hot managed code using dynamic profiles. |
| `SDV_TEXTURE_UPLOAD_SYNC` | `0` | Avoid redundant GPU waits; use `1` to restore them if rendering problems occur. |
| `SDV_SPRITE_BATCH_CAPACITY` | `2048` | Initial sprite pool; grows normally for larger scenes. |
| `SDV_STARTUP_DIAGNOSTICS` | `1` | Log managed startup, CPU eligibility and framework stages. |
| `SDV_FORCE_PREPARE` | `0` | `1` bypasses preparation caching. Reset it to `0` after a forced preparation launch. |

The old `SDV_PERFORMANCE_OVERLAY` setting is superseded and ignored, so an existing value of `0` no longer hides the battery meter. Select the battery HUD in KNULLI and leave `SDV_HUD=auto`. Performance governor mode can use more battery and generate more heat; the launcher restores the original governor on handled exit.

Keep the shipped performance defaults for normal play. Further investigation of the startup delay is deferred at the owner’s request; no extra tuning is required.

The cache compares file size, modification/change timestamps and inode metadata rather than hashing all DLL contents. If you deliberately replace files while preserving their metadata, force preparation or delete `stardewvalleymainline/.preparation-cache`.

## Saves and mods

- Persistent saves and settings live under `stardewvalleymainline/savedata/`. Back up this folder regularly with the game closed.
- To transfer a compatible PC save, copy its complete farm folder into `savedata/Saves/`. Match the game version and required mods before loading it.
- Install user SMAPI mods in `stardewvalleymainline/Mods/`, not `gamedata/Mods/`. Vanilla is the default when no user mods are present.
- Windows/x64 native dependencies, mismatched SMAPI APIs and large mod packs may prevent a mod from working on this ARM64 device.

## Troubleshooting and performance reports

The main log is `stardewvalleymainline/log.txt`.

| Symptom | First check |
| --- | --- |
| Slow startup | Look for `Preparation cache hit` on a repeat launch. Compare `Startup preparation`, total launcher time and game-start/patch-initialization timestamps. |
| Battery HUD missing | Select the HUD in KNULLI, use `SDV_HUD=auto` and launch through the KNULLI menu. An SSH launch may lack HUD context. |
| Exit status 135/139 | The owner confirmed status 135 after deliberately exiting with Fn+Start, not an unexpected closure during play. Its native cause is unconfirmed. For an unexpected closure, report the scene and final log. |
| Cache never hits | Check for changed game/mod files, `SDV_FORCE_PREPARE=1`, or the cache-unavailable message. |
| Missing/incomplete game files | Check the `gamedata/` layout and copy a clean mainline installation. |
| `MonoGame.Framework.dll` reported in use at `CopyOverlayFiles` | Upgrade from v0.1.0; v0.1.1 and later close Cecil dependency handles before copying. |
| Rumble absent | Enable the in-game option and `SDV_RUMBLE`; check player index and `RG34XXSP KNULLI rumble ready` in the log. |
| `glxinfo: command not found` | PortMaster’s KNULLI GL script can fall back to GLES. This line alone is not a startup failure. |
| A mod fails | Test vanilla, then check its game/SMAPI requirements and dependencies. |

For diagnostics over SSH, with the game **closed**:

```sh
cd /userdata/roms/ports/stardewvalleymainline
bash tools/diagnose-rg34xxsp
bash tools/diagnose-rg34xxsp --rumble-test
```

The second command produces a 150 ms motor pulse. A missing or unwritable motor interface requires firmware with working RG34XXSP rumble support.

For deeper profiling, create `profiling.enabled` in the port folder before launching. Logs are written under `logs/`; remove the marker after recording because profiling adds overhead. Compare the same save/location with the same mods and display mode. Hardware speed gains have not been measured by this repository’s automated tests.

[Open an issue](https://github.com/cdale11/sv-port/issues/new) with the release, KNULLI version, game version and Windows/Linux source, mod list, reproduction steps and relevant log. For loading reports, include first- and second-launch times. Avoid including private save contents unless intentionally sharing them.

## Build, repository layout and contributing

A retail-free cross build requires Docker and Git:

```sh
bash build/scripts/build-rg34xxsp.sh
```

Artifacts are written to `build/out/mainline/release/`. Building uses .NET SDK 8; the game runs on bundled **.NET 6.0.36**. MonoGame is pinned and prepared automatically. See the [build guide](https://github.com/cdale11/sv-port/blob/main/build/README.md) for inputs, tests, optional retail smoke tests and release publishing.

| Path | Purpose |
| --- | --- |
| `portmaster_stardewvalley_mainline/` | Launcher, device config and install template |
| `build/src/`, `build/rg34xxsp/`, `build/patches/` | Importer, native helpers, managed rumble bridge and MonoGame patch |
| `build/scripts/`, `build/docker/` | Build and packaging tools |
| `tests/` | Launcher, rumble, import and package regression checks |
| `docs/` | Credits, current release notes and validation record |
| `scripts/` | Profiling comparison and PortMaster tree export |
| `.github/workflows/` | Automated checks and release packaging |

Read [CONTRIBUTING.md](https://github.com/cdale11/sv-port/blob/main/CONTRIBUTING.md) before submitting changes. The project direction, repository and hardware feedback come from **cdale11**; the device adaptation, fixes, tests and documentation were developed with **OpenAI Codex assistance**. Upstream contributors are credited separately in [CREDITS.md](docs/CREDITS.md).

## License and acknowledgements

The port source is [MIT licensed](LICENSE), retaining upstream notices. Bundled runtimes and libraries have their own licenses under `stardewvalleymainline/licenses/`; the packaged .NET runtime also retains its notices. Stardew Valley remains ConcernedApe’s separately licensed game.

Thanks to **ConcernedApe**, **Producdevity**, **Johnny on Flame**, **MonoGame**, **PortMaster** and **KNULLI**. This fork is a community project and is not an official ConcernedApe or Anbernic release.
