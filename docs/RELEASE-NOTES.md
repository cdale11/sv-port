**v0.1.3 adds CPU/rendering optimizations and restores KNULLI's selected battery HUD.**

- Request all online cores and temporarily use the supported performance governor; restore the previous governor on handled exit. No overclock is applied. Main simulation/rendering remains single-threaded.
- Enable tiered JIT, quick loop compilation and dynamic PGO; remove artificial processor-count caps and align imported runtime configuration.
- Remove redundant GPU-wide texture upload waits and start with a larger sprite pool, which still grows for bigger scenes. These reduce framework overhead; large-farm FPS improvements have not been measured.
- Preserve KNULLI battery/performance HUD context by default (`SDV_HUD=auto`). The old `SDV_PERFORMANCE_OVERLAY=0` no longer hides it. Helpers do not load the HUD.
- Fix built-in shader resource names and stop audio workers before native teardown. The reported exit status 139 has not been reproduced on the handheld; this is not a confirmed fix for its cause.
- Add early managed startup, CPU eligibility and framework phase logs to locate the remaining startup delay.
- Retain preparation caching, Windows/Linux mainline import handling and built-in rumble.

**Upgrade:** close the game, back up saves, then extract `stardewvalley-rg34xxsp-performance-update.zip` into `/userdata/roms/ports/`, allowing replacement. Includes launcher/helpers, importer, patch DLL and MonoGame overlay. Does not include or overwrite retail game files, saves, mods or your configuration. Missing new config settings use their defaults. Select the battery HUD in KNULLI and launch through its menu. Compare loading on the second launch.

**Fresh install:** use `stardewvalleymainline.zip`, then supply a clean owned Windows or Linux Steam .NET 6 mainline installation. No retail files are included.

**Validation:** 27 Python checks, managed import and rumble integration checks, package validation and .NET 6.0.36 SDL/OpenGL graphics/audio smoke tests passed. Graphics tests cover full/partial texture ordering, 8192 sprites, pool growth and worker shutdown with synchronization enabled and disabled. The owner reports gameplay/rumble working on KNULLI Scarab using recent Windows mainline files without mods. v0.1.3 handheld speed, battery HUD and exit behavior remain to be checked. Performance governor mode can increase heat and battery usage.

[Install/configure](https://github.com/cdale11/sv-port#readme) · [Performance settings](https://github.com/cdale11/sv-port/blob/main/docs/PERFORMANCE.md) · [Hardware validation](https://github.com/cdale11/sv-port/blob/main/docs/HARDWARE-VALIDATION.md)
