# Changelog

All releases remain experimental. Loading-time and FPS improvements are not yet
measured on the handheld. See [hardware validation](docs/HARDWARE-VALIDATION.md).

## Unreleased — documentation maintenance

- Record v0.1.3 battery HUD confirmation, four-core eligibility, game assembly
  version, measured startup stages and deliberate Fn+Start exit context.
- Document normal quit/save behavior and defer further startup tuning.
- Add focused issue and pull-request templates; retain existing release archives.

## v0.1.3 — CPU, rendering and battery HUD

[Release and downloads](https://github.com/cdale11/sv-port/releases/tag/rg34xxsp-knulli-v0.1.3)

- Request all online CPU cores and temporarily select the supported performance
  governor, restoring its previous value on handled exit.
- Enable dynamic PGO and quick tiered JIT, align imported runtime configuration
  and remove inherited artificial processor-count caps.
- Preserve KNULLI's selected battery HUD by default; supersede the old overlay switch.
- Avoid redundant GPU-wide texture upload waits and preallocate larger sprite pools.
- Correct embedded built-in shader names in the SDK framework build.
- Join audio workers before native teardown; the handheld exit-139 cause remains unconfirmed.
- Add early startup/framework-stage diagnostics and real .NET 6 graphics/audio tests.
- Include the updated MonoGame overlay in the non-destructive upgrade package.
- Consolidate the README, credits, contribution/build guides and package documentation.

## v0.1.2 — startup and overlay optimizations

[Release and downloads](https://github.com/cdale11/sv-port/releases/tag/rg34xxsp-knulli-v0.1.2)

- Cache successful preparation and skip the patcher for unchanged dependencies.
- Invalidate the cache for relevant game/native/overlay/patcher and SMAPI changes;
  fall back to preparation if metadata cannot be inspected.
- Read PE/CLI headers rather than complete DLLs for architecture checks.
- Avoid normalizing unused mods in vanilla mode and recopying SMAPI defaults on
  cache hits.
- Make inherited MangoHud injection opt-in while preserving other preloads.
- Add startup phase durations and initialization timestamps.
- Provide a performance upgrade ZIP that preserves game files, saves, mods and
  configuration.

## v0.1.1 — DLL-lock startup fix

[Release and downloads](https://github.com/cdale11/sv-port/releases/tag/rg34xxsp-knulli-v0.1.1)

- Dispose Cecil’s cached dependency assemblies before overlay copying, including
  exception paths.
- Add a regression fixture reproducing and checking the locked-DLL failure.
- Provide a patcher-only hotfix ZIP.
- Gameplay and internal rumble subsequently reported working by the device owner.

## v0.1.0 — initial RG34XXSP / KNULLI prerelease

[Release and downloads](https://github.com/cdale11/sv-port/releases/tag/rg34xxsp-knulli-v0.1.0)

- Adapt the mainline PortMaster launcher to RG34XXSP/KNULLI.
- Add an internal motor bridge and independent timeout/exit shutdown helper.
- Implement Windows/Linux .NET 6 mainline import handling and device defaults.
- Retain optional upstream SMAPI, controller mapping and persistent saves.
- Add ARM64 cross builds, component checks, diagnostics and checksummed packages.
