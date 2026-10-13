# RG34XXSP / KNULLI validation

## User-reported device result

The device owner reports the game working well, with built-in rumble and battery
HUD on RG34XXSP running **KNULLI Scarab**, Windows mainline files and no mods.
The v0.1.3 log identifies the game assembly as **1.6.15.24356**.

| Observed on v0.1.3 | Result |
| --- | --- |
| Repeat-launch preparation | Cache hit, 0 seconds |
| .NET CPU eligibility | Four processors; affinity `0–3`; workstation GC; PGO enabled |
| SDL/audio setup | Under one second after the framework constructor marker |
| Remaining startup gap | About 32.2 seconds after audio setup and before game initialization |
| Game initialization | About 5.7 seconds |
| Launch to first completed tick | About 42 seconds; this is not a measured time to title or farm load |
| Battery HUD | Confirmed visible by owner |
| Native exit status | 135 (bus error), following deliberate **Fn+Start** exit |

The owner clarified that the game did not close unexpectedly during play. The
native cause of the hotkey-exit error remains unconfirmed; it is not recorded as
a clean native shutdown or a fixed bug. Earlier status 139 also remains unresolved.
The log does not prove the performance governor changed or establish maximum
sustained frequency, temperature, battery drain or large-farm FPS.

Further startup tuning is deferred at the owner's request because gameplay is
working well. Keep the current release and defaults. Windows gameplay is
user-reported; Linux retail gameplay, exact feature coverage and the remaining
hardware checks below are still pending.

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

## Remaining hardware checks (not a requirement for current use)

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
