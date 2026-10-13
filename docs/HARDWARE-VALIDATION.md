# RG34XXSP / KNULLI validation

## Completed in the build environment

- Patched MonoGame DesktopGL, importer and patch assembly compile.
- ARM64 native binaries built with Debian 11 cross tools; glibc symbol ceiling checked.
- Windows/Linux runtime-pack fixtures, repeat imports, runtime-major rejection and native architecture filtering pass.
- Launcher mock tests cover vanilla launch, 720×480 fallback, platform hook, failed patching, invalid mode, cleanup and game exit status.
- Motor helper tests cover commands, timeout, pipe EOF, SIGTERM, missing device and malformed input.
- Managed-to-native integration covers player routing, both vibration channels, stop, timeout and close.
- Package checked for expected payloads and absence of retail game files.

These checks do not prove gameplay, frame rate or real motor behavior.

## Required on the handheld (not yet performed)

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

Do not promote this build from experimental until both retail imports and the
core hardware checks pass. Report `log.txt` and diagnostics with failures; omit
private save contents unless intentionally sharing them.
