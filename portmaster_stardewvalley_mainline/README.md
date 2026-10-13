# Stardew Valley — RG34XXSP / KNULLI

Experimental ARM64 port for the **Anbernic RG34XXSP running KNULLI**, based on
[Producdevity's mainline port](https://github.com/Producdevity/portmaster-stardew-valley-mainline).
Windows and Linux Steam mainline imports are implemented. This is a compiled,
retail-free test release: gameplay with either retail build and testing on a real
RG34XXSP are still required. No FPS or full compatibility claim is made.

## Download and install

1. Install/update PortMaster on 64-bit KNULLI. Download the ZIP from
   [Releases](https://github.com/cdale11/sv-port/releases), not GitHub's source ZIP.
2. Extract into `/userdata/roms/ports/` (the network share is `roms/ports`). The
   launcher must sit beside `stardewvalleymainline/`, without another enclosing folder.
3. Copy a **complete, clean Windows or Linux Steam mainline installation** into
   `stardewvalleymainline/gamedata/`. Both use `Stardew Valley.dll`,
   `Stardew Valley.deps.json`, `Stardew Valley.runtimeconfig.json`, and `Content/`.
   Do not mix Windows/Linux files or use the legacy compatibility beta. The copied
   files are patched in place, so keep the original PC installation as your backup.
4. Refresh KNULLI's game list and launch **StardewValleyMainline** from Ports.
   First launch includes patching and may take longer.
5. Enable controller vibration in Stardew's settings. Install mods in
   `stardewvalleymainline/Mods/` if desired; leave it empty for vanilla.

If replacing an existing mainline port, back up its entire folder first, especially
`savedata/`, `Mods/` and `rg34xxsp.conf`. This fork deliberately keeps the upstream
installation directory and is not a second side-by-side mainline installation.
When switching game versions or Windows/Linux sources, replace `gamedata/` with a
clean copy; keep `savedata/` and `Mods/` separate.

### v0.1.1 upgrade for the locked MonoGame DLL error

If v0.1.0 aborts at `CopyOverlayFiles` with `MonoGame.Framework.dll` in use, close
the game and extract `stardewvalley-rg34xxsp-patcher-hotfix.zip` from Releases into
`/userdata/roms/ports/`, allowing replacement. This ZIP contains only the patcher
folder under `stardewvalleymainline/tools/`; existing game files, saves, mods and
configuration stay in place. Launch again. The patcher now disposes Cecil's cached
dependency assemblies before overlay copying. A fresh game import is unnecessary
for this specific failure.

## Device support

| Feature | Implementation and current limit |
| --- | --- |
| Display | Native 720×480 fallback; uses PortMaster's dimensions for HDMI. Upstream UI/viewport patches retained. |
| Controls | KNULLI/PortMaster SDL mappings; sticks, D-pad, triggers, face buttons, cursor and exit helper retained. No hard-coded controller GUID. |
| Built-in rumble | Stardew `GamePad.SetVibration` connects to KNULLI's H700 motor interface. Binary on/off motor; magnitudes collapse to on/off. |
| Rumble cleanup | Separate ARM64 helper stops on pipe EOF, termination or 250 ms without a command. Active calls refresh at most every 50 ms. An isolated long request is capped at 250 ms. |
| External controllers | Existing SDL haptics retained for other player indices. Internal motor is player index 0 by default; configurable. |
| Memory | Workstation GC, tiered compilation and ReadyToRun enabled; full .NET runtime retained for reflection/mod compatibility. No overclock or global system changes. |
| Saves | PortMaster persistent save binding to `savedata/`; compatible PC saves can be copied with the game closed. |
| Mods | Upstream SMAPI 4.5.2 bundle, automatic/forced/disabled modes. Individual mods remain version-, API- and memory-dependent. |
| Lid/sleep, Wi-Fi, audio, HDMI | KNULLI platform integration retained. Suspend/resume, audio recovery, HDMI and LAN play need physical-device testing. Lid-close does not create a save. |
| Steam services | Upstream Steam API stubs remain. Steam overlay, achievements, cloud saves and Steam invite multiplayer are unavailable. LAN multiplayer is unverified. |

The importer supports the **.NET 6 mainline layout** (including runtime packs
6.0.32–6.0.36) and retains upstream's Stardew 1.6 IL patches. It rejects another
runtime major or an unrecognized game-method layout instead of claiming every
future update works. Linux/Windows import fixtures pass; no retail game files were
available for gameplay validation. GOG and the compatibility branch are untested.

## Configuration and troubleshooting

Edit `stardewvalleymainline/rg34xxsp.conf`:

- `SDV_LAUNCH_MODE=auto`, `vanilla`, or `smapi`.
- `SDV_RUMBLE=1` enables the built-in bridge; `0` leaves only SDL controller rumble.
- `SDV_RUMBLE_PLAYER=0` selects the zero-based controller index for the built-in motor.

The game writes `stardewvalleymainline/log.txt`. Look for
`RG34XXSP KNULLI rumble ready` when the game initializes vibration support.
With the game **closed**, run over SSH:

```sh
cd /userdata/roms/ports/stardewvalleymainline
bash tools/diagnose-rg34xxsp
bash tools/diagnose-rg34xxsp --rumble-test
```

The second command emits a 150 ms motor pulse. If the interface is missing or
unwritable, use a KNULLI build with working RG34XXSP rumble support; the port does
not alter system device permissions. If the pulse works but game rumble does not,
check the in-game vibration option, player index and launch log.

For performance comparisons, create `profiling.enabled` in the port folder to
use upstream frame/content profiling. Delete it after recording, as logging adds
work. Start with vanilla and compare the same save/location before adding mods.
See [hardware validation](docs/HARDWARE-VALIDATION.md) for outstanding checks.

## Build and test

Requires Docker and Git. The retail-free build uses host .NET SDK 8 for compilation
and Debian 11 ARM64 cross tools for native code; the game itself runs on bundled
**.NET 6.0.36**, not .NET 8. MonoGame is pinned and patched automatically.

```sh
bash build/scripts/build-rg34xxsp.sh
# build/out/mainline/release/stardewvalleymainline.zip
```

For optional retail smoke tests, use an ARM64 Docker runtime (native or emulated):

```sh
SDV_GAME_DIR='/path/to/clean/Steam/game' bash build/scripts/build-mainline.sh
```

Run the Windows and Linux copies separately. Each run stages its own copy for
validation; retail files must never be committed or included in release archives.
GitHub Actions runs component checks on pushes. Manually run **Port checks** to
also build and upload the install ZIP as an Actions artifact.

```sh
python3 -m unittest discover -s tests -v
dotnet run --project tests/ImportTests -c Release
gcc -std=c11 -O2 -Wall -Wextra -Werror build/src/native/rg34xxsp_rumble.c -o /tmp/rg34xxsp-rumble
dotnet run --project tests/RumbleIntegration -c Release -- /tmp/rg34xxsp-rumble
```

## Provenance

- Mainline port: `986097ce2e586abb799c40dfd7386b288fa8ae1b` (history and licenses retained).
- MonoGame: `3874df98675039d368e77dca50d9a00ac970537e`, plus the checked-in rumble patch and bridge.
- KNULLI motor contract: [H700 knulli-rumble](https://github.com/knulli-cfw/distribution/blob/ab3a6809286b3cff1648fb8c96a0200bc869beb6/board/batocera/allwinner/h700/fsoverlay/usr/bin/knulli-rumble),
  `/sys/class/power_supply/axp2202-battery/moto`.
- .NET 6.0.36, SMAPI 4.5.2, SkiaSharp 2.80.3; licenses packaged alongside the runtime.

Thanks to ConcernedApe, Producdevity, Johnny on Flame, MonoGame, PortMaster and
KNULLI. Stardew Valley is sold separately; this repository contains no retail game.
