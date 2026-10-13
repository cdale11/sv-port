**v0.1.1 fixes the startup abort at `CopyOverlayFiles` where `MonoGame.Framework.dll` is reported as being in use.** The patcher now closes Mono.Cecil's cached dependency assemblies before copying overlays, including exception paths. A regression fixture reproduces the held handle and verifies replacement after cleanup.

**Upgrading v0.1.0:** close the game, extract `stardewvalley-rg34xxsp-patcher-hotfix.zip` into `/userdata/roms/ports/`, and allow replacement. The hotfix contains only `stardewvalleymainline/tools/MainlineGameDataPatcher/`; game files, saves, mods and configuration are untouched. Launch again; this failure does not require recopying your game installation.

The bind-mount message and PortMaster's missing `glxinfo` warning were not the fatal error. KNULLI's PortMaster GL script falls back to GLES when its GL probe fails.

Experimental Stardew Valley mainline port for **Anbernic RG34XXSP running KNULLI**.

- Windows and Linux Steam .NET 6 mainline import paths.
- Built-in H700 rumble connected to MonoGame vibration calls, with an independent timeout/exit cleanup helper.
- 720×480 display fallback, PortMaster controls and platform hooks, persistent saves, and optional bundled SMAPI 4.5.2.
- ARM64 .NET 6.0.36 runtime and native libraries included; no retail game files.

**Validation:** component builds, 11 launcher/native-helper tests, managed import fixtures, managed-to-native rumble integration, and ZIP architecture/checksum checks passed. No retail game files or physical RG34XXSP were available. Gameplay on both retail builds, actual rumble, performance, HDMI, LAN and lid suspend/resume remain unverified. Steam services remain unavailable through upstream stubs.

**Install:** extract `stardewvalleymainline.zip` into KNULLI's `/userdata/roms/ports/`, then copy a complete clean Windows or Linux Steam mainline installation into `stardewvalleymainline/gamedata/`. Do not mix platform files or use the compatibility beta. Back up an existing port before installing. Enable vibration in the game.

Read the [installation and configuration guide](https://github.com/cdale11/sv-port#readme) and [hardware checklist](https://github.com/cdale11/sv-port/blob/main/docs/HARDWARE-VALIDATION.md). The source archive alone is not installable. The `.sha256` file verifies the install ZIP; a per-file `SHA256SUMS` is also inside it.
