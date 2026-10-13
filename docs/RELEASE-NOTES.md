**v0.1.2 reduces repeat-launch preparation work and removes optional overlay hooks by default.**

- A metadata cache skips starting the .NET patcher when game dependencies, patcher/overlay inputs and SMAPI mode are unchanged. Changes automatically trigger preparation; errors fall back to the normal path.
- SMAPI default mods are copied only during preparation. Unused mods are not normalized in vanilla mode.
- DLL architecture checks read PE/CLI headers rather than loading whole files. The synthetic 64 MiB fixture allocates approximately 4.5 KB for this check.
- Inherited MangoHud preload entries are filtered by default. Other preload libraries and the rumble bridge are preserved. `SDV_PERFORMANCE_OVERLAY=1` retains the overlay.
- Startup logs now separate preparation, launcher time and game initialization. `SDV_FORCE_PREPARE=1` forces a preparation pass.
- Includes the v0.1.1 Cecil dependency-handle fix.

**Existing installations:** close the game and extract `stardewvalley-rg34xxsp-performance-update.zip` into `/userdata/roms/ports/`, allowing replacement. It contains only the launcher, preparation helpers, patcher and patch DLL. It preserves your game files, saves, mods and config. New settings have defaults, so you need not replace your config. First launch prepares/cache-stamps the files; compare the second launch to v0.1.1.

**Fresh installs:** use `stardewvalleymainline.zip`, then copy a clean Windows or Linux Steam .NET 6 mainline installation into `stardewvalleymainline/gamedata/`.

**Validation:** 21 launcher/native-helper tests, managed import and DLL-handle fixtures, header-scan allocation checks and package checks passed locally. A user confirmed v0.1.1 gameplay and rumble on RG34XXSP/KNULLI. v0.1.2 loading-time and FPS gains have not been measured on hardware. Steam services remain unavailable; formal retail-version and suspend/resume coverage is pending.

[Installation/configuration guide](https://github.com/cdale11/sv-port#readme) · [Hardware checks](https://github.com/cdale11/sv-port/blob/main/docs/HARDWARE-VALIDATION.md)
