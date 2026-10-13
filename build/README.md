# Build and validation guide

The build creates a retail-free ARM64 PortMaster install ZIP and an upgrade ZIP
for RG34XXSP/KNULLI. The SDK used for compilation is separate from the .NET runtime
that executes the game.

## Inputs and dependencies

| Input | Version or location |
| --- | --- |
| Mainline port base | `986097ce2e586abb799c40dfd7386b288fa8ae1b` |
| MonoGame | `3874df98675039d368e77dca50d9a00ac970537e` on `Producdevity/MonoGame`, plus the checked-in rumble patch/bridge |
| .NET game runtime | 6.0.36, Linux ARM64 |
| SMAPI | 4.5.2 |
| SkiaSharp native assets | 2.80.3 |
| Native compiler | Debian 11 ARM64 cross tools; older glibc baseline checked during compilation |
| Install template | `portmaster_stardewvalley_mainline/` |
| Optional retail validation input | `SDV_GAME_DIR`, or `stardewvalley_steam/Stardew Valley/` |

See [credits](../docs/CREDITS.md) for upstream sources and notices. Retail game
files are required only for gameplay smoke tests, never for a distributable ZIP.

## Retail-free cross build

Requires Docker and Git. Run from the repository checkout:

```sh
bash build/scripts/build-rg34xxsp.sh
```

The script prepares the pinned MonoGame checkout at `../MonoGame/`, bootstraps its
public dependencies, cross-compiles native ARM64 components, and compiles/packages
managed components using .NET SDK 8. Set `MONOGAME_DIR` to use another checkout.
The preparation script rejects a conflicting revision or patch rather than
silently overwriting it.

Artifacts appear under `build/out/mainline/release/`:

- `stardewvalleymainline.zip`: complete install package, without retail game files.
- `stardewvalley-rg34xxsp-performance-update.zip`: launcher/helpers/patcher/patch
  DLL only; preserves the existing game files, saves, mods and config.
- `.sha256` files and the full ZIP’s `.contents.txt` listing.

The full package contains `BUILD-STATUS.txt` and per-file `SHA256SUMS`. This build
**skips retail gameplay smoke tests**. Successful compilation does not establish
frame rate, game-version coverage or suspend/resume behavior.

## Component checks

Requires Python 3, GCC and .NET SDK 8 on the build host:

```sh
python3 -m unittest discover -s tests -v
dotnet run --project tests/ImportTests -c Release
gcc -std=c11 -O2 -Wall -Wextra -Werror build/src/native/rg34xxsp_rumble.c -o /tmp/rg34xxsp-rumble
dotnet run --project tests/RumbleIntegration -c Release -- /tmp/rg34xxsp-rumble
dotnet build build/src/MainlineGameDataPatcher -c Release
python3 tests/check_package.py build/out/mainline/release/stardewvalleymainline.zip
```

Coverage includes launcher lifecycle, cache invalidation/fallback, overlay
filtering, Windows/Linux runtime fixtures, DLL dependency-handle cleanup,
header-only architecture scans, motor watchdog behavior and managed/native rumble
routing. Package checks verify expected files, checksums, ARM64 native binaries,
executable permissions and exclusion of retail data.

## Optional retail smoke tests

Requires ARM64 Docker execution, either native or through configured emulation,
and a clean owned Steam mainline game installation. Prepare the pinned framework
checkout first with `build/scripts/prepare-rg34xxsp-framework.sh` if needed.

```sh
SDV_GAME_DIR='/path/to/clean/Steam/game' bash build/scripts/build-mainline.sh
SDV_GAME_DIR='/path/to/clean/Steam/game' bash build/scripts/build-mainline.sh --trace-validation
```

Each run stages a copy. Run Windows and Linux imports separately. Validation
checks vanilla startup, forced SMAPI, default-mod mode selection and a fixture mod
covering dynamic audio/content loading. Existing staged validation can be rerun:

```sh
bash build/scripts/validate-mainline.sh --skip-image
```

The build records smoke-test results under `build/out/mainline/logs/`. The device
checklist remains separate: [hardware validation](../docs/HARDWARE-VALIDATION.md).

## Preparation on the device

The launcher checks dependency metadata and skips the patcher when its successful
cache matches. A cold/invalidated preparation validates the .NET 6 layout, patches
game methods, removes conflicting desktop runtime/native files, copies the
MonoGame overlay, stages SMAPI when needed and normalizes managed architecture.
Cecil readers and cached dependency assemblies close before file replacement.

Canonical user documentation is copied from the root README and `docs/` during
staging. Do not commit duplicate package documentation.

## GitHub Actions and release maintenance

Pushes and pull requests run component checks. Manually dispatch **Port checks**
to run checks, build packages, upload Actions artifacts and publish the configured
prerelease. The release tag/title are currently defined in
`.github/workflows/checks.yml`; update them with `docs/RELEASE-NOTES.md`,
`CHANGELOG.md` and README links when releasing a new version. Rerunning the workflow
can replace assets on that configured release, so confirm the intended version.

## Additional tools

```sh
csi build/scripts/audit-monogame-api.csx
csi build/scripts/scan-mod-monogame-refs.csx /path/to/Mods
bash scripts/export-portmaster-tree.sh /path/to/portmaster-new
```

The first two require a C# interactive host and inspect compatibility. Export uses
a built release-package tree and `rsync`; it does not submit a PortMaster PR.
Follow PortMaster’s own repository checks and hardware requirements before
submitting an upstream contribution.

Generated artifacts, dependency caches, reports, test `bin/obj` trees and local
retail installations are ignored. Keep them out of commits and release payloads
unless they are an intentional, reviewed distributable artifact.
