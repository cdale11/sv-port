# Changelog

All releases remain experimental. Loading-time and FPS improvements are not yet
measured on the handheld. See [hardware validation](docs/HARDWARE-VALIDATION.md).

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

## Unreleased — repository maintenance

- Consolidate installation, controls, configuration and troubleshooting in the
  README; document fork contributions and upstream provenance.
- Refresh contribution/build guides, add release history and public repository
  metadata, and copy canonical documentation during package staging.
- Ignore local retail files, saves, mods and runtime logs in the install template.
