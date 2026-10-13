# RG34XXSP / KNULLI validation

## User-reported device result

The device owner reports gameplay and built-in rumble working on RG34XXSP with
**KNULLI Scarab**, recent Windows mainline game files and no mods. Exact game
version was not supplied. A v0.1.2 repeat-launch log shows preparation cached in
0 seconds, then about 34 seconds before patch initialization. Exit status 139 was
logged, but the user reports no in-game crash. Battery HUD was missing.

v0.1.3 addresses HUD filtering, CPU/runtime/rendering overhead and an audio
shutdown race. Its handheld loading times, large-farm frame rates, HUD restoration
and exit behavior still need verification.

## Completed in the build environment

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

These automated checks do not establish hardware frame rate or loading-time gains.

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
