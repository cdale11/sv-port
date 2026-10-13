# Quality assurance and compatibility

## Release scope

This release targets **Anbernic RG34XXSP with 64-bit KNULLI** and Stardew Valley
1.6 Windows/Linux Steam mainline installations using .NET 6. The reference
configuration is KNULLI Scarab with game assembly 1.6.15.24356, vanilla mode,
internal rumble and KNULLI battery HUD.

Windows and Linux import layouts share the automated importer checks below.
Those fixtures validate conversion and runtime handling; they are not exhaustive
retail gameplay coverage. Firmware-dependent features and mod combinations
should be checked with the regression checklist when changing dependencies.

## Known limitations

- Cached preparation can complete in 0 seconds, while game startup still takes
  tens of seconds. A reference repeat-launch log has a 32.2-second gap between
  audio setup and game initialization, and about 42 seconds to the first completed
  tick. This is not a time-to-title or farm-load benchmark.
- All four cores are eligible for existing background work. The main simulation
  and render loop remain single-threaded; large-farm frame rates vary.
- Fn+Start can end with native status 135 or 139. Use the in-game quit option for
  normal shutdown. The hotkey does not save progress. The native error's cause
  is unresolved; an unexpected closure during play should be reported separately.
- Firmware frequency limits, thermal controls and permissions govern CPU policy.
  Core eligibility does not imply a guaranteed sustained clock speed.
- Steam overlay, achievements, cloud saves and Steam-invite multiplayer are not
  provided. Legacy compatibility-branch game files are unsupported.
- Suspend/resume, HDMI, external controllers and LAN multiplayer depend on firmware
  and configuration; comprehensive coverage of these combinations is not available.
- Future game versions, GOG layouts and native mod dependencies may need additional
  compatibility work. Individual mods are not covered by the core release checks.

## Automated release checks

- Patched MonoGame DesktopGL, importer and patch assembly compile.
- ARM64 native binaries built with Debian 11 cross tools; glibc symbol ceiling checked.
- Windows/Linux runtime-pack fixtures, repeat imports, runtime-major rejection and native architecture filtering pass.
- Cecil dependency resolution reproduces a held DLL handle; overlay replacement succeeds after closing the rewrite scope, including exception paths.
- Launcher mock tests cover vanilla launch, 720×480 fallback, platform hook, failed patching, invalid mode, cleanup and game exit status.
- Motor helper tests cover commands, timeout, pipe EOF, SIGTERM, missing device and malformed input.
- Managed-to-native integration covers player routing, both vibration channels, stop, timeout and close.
- Cache regression tests cover unchanged launches, game/native/overlay/patcher changes, SMAPI changes, failure fallback and forced preparation.
- Architecture scans allocate about 4.5 KB for a synthetic 64 MiB DLL. This measures allocation, not device loading time.
- HUD auto mode retains KNULLI battery configuration and non-HUD preloads; explicit off mode filters MangoHud.
- CPU policy fixtures check all-core eligibility, fallbacks, governor restoration and later firmware changes.
- .NET 6.0.36 SDL/OpenGL tests pass with both upload synchronization modes: full/partial texture ordering, 8192 sprites, pool growth and stopped audio workers after disposal.
- Package checked for expected payloads and absence of retail game files.

These checks validate component behavior and package integrity. They do not provide a frame-rate guarantee for every retail version, save or mod combination.

## Hardware regression checklist

Record KNULLI version, game version/store/Windows or Linux source, archive checksum,
mods, display mode and save location with each result.

1. Start clean Windows mainline, create a farm, enter/leave buildings, play through
   a day, save, quit, relaunch and load. Repeat with clean Linux mainline files.
2. Check text entry, all controls, right-stick cursor, pause and PortMaster exit.
3. Run the diagnostic motor test, then verify in-game vibration (including fishing)
   with vibration enabled and disabled. Confirm it stops after quitting or crashing.
4. Close/open the lid in a paused game, repeat during normal play, and verify display,
   controls, sound and motor state. Use KNULLI's suspend setting, not shutdown.
   Save at end of day before testing; lid-close cannot replace an in-game save.
5. Test speaker/headphone audio, HDMI resolution, and USB/Bluetooth pad reconnection.
6. Run vanilla, forced SMAPI without user mods, then a small compatible mod set.
7. Test LAN co-op with matching game versions. Steam services are unavailable.
8. Compare frame-time profiles on the same save across indoor/outdoor scenes,
   rainy weather, busy farm and a full day. Record memory use and temperatures.

For regressions, include `log.txt`, firmware/game versions, configuration and
reproduction steps. Do not include retail game files or private save contents.
