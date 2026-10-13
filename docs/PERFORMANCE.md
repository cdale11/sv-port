# Performance and battery HUD

The RG34XXSP has four CPU cores. The launcher requests every online core that the
kernel permits, removes an inherited `DOTNET_PROCESSOR_COUNT` override and makes
at least that many thread-pool workers eligible. This benefits existing audio,
JIT and background work. It does not divide Stardew's main update/render loop
across four cores or change simulation/save behavior.

The supported `performance` CPU governor is selected only while the game runs.
Firmware frequency limits and thermal controls remain intact. Original governors
are restored on handled exit unless firmware changed them in the meantime.
Power loss or forcibly killing the launcher cannot execute cleanup. Set
`SDV_CPU_GOVERNOR=inherit` for firmware-controlled power policy, and
`SDV_CPU_AFFINITY=inherit` to preserve its affinity.

## Startup and hot code

Preparation caching already reduces repeat preparation to near zero. The v0.1.3 device log shows a
32.2-second interval after audio setup and before game initialization;
it is not a repeat patching delay. The current markers do not distinguish retail
construction, static initialization, compilation or other setup in that gap. v0.1.3 logs `Managed startup`, framework stages,
CPU count, affinity and entry assembly identity to locate this interval.

Tiered compilation first runs quick code, then recompiles hot code in the
background. Dynamic PGO uses runtime profiles to optimize that hot code. Quick
loop compilation can shorten initial compilation but can run slower until a
method is promoted. Defaults enable all three; compare the same save after a
warm-up, not only its first frame. Further startup tuning is deferred because the owner reports the game working
well. Keep the shipped defaults; the suggested quick-loop comparison was not
reported as performed and is not a confirmed improvement. Optional diagnostic
fallbacks in `rg34xxsp.conf`:

```sh
SDV_TIERED_PGO=0
SDV_QUICKJIT_LOOPS=0
# Disable tiered compilation entirely only for comparison:
SDV_TIERED_COMPILATION=0
```

Runtime configuration changes trigger preparation again. Workstation GC remains
enabled for this memory-limited device. ReadyToRun consumption remains enabled;
this does not precompile retail game assemblies.

## Rendering and large farms

Texture uploads on the existing UI OpenGL context no longer wait for the entire
GPU after every full/partial upload. Same-context ordering is preserved. If you
see corruption, set `SDV_TEXTURE_UPLOAD_SYNC=1` to restore those waits and report
the affected scene. This is not parallel texture upload.

The initial sprite pool is 2048 instead of 256 to reduce allocations in busy
scenes. It grows normally, retains draw order and does not limit farm size.
`SDV_SPRITE_BATCH_CAPACITY=256` restores the original starting capacity; accepted
values are clamped to 256–4096. CPU-heavy simulation, pathfinding, mods and memory
pressure can still dominate on a large farm. No objects, crops, animals, effects
or simulation ticks are removed to improve performance.

## Battery HUD

Choose the battery display in KNULLI's HUD settings and launch from its Ports
menu. `SDV_HUD=auto` preserves inherited MangoHud preload/configuration, including
`MANGOHUD_CONFIGFILE`. An SSH launch may not receive that menu context.
`SDV_HUD=off` explicitly disables it; `on` enables injection using the available
configuration. The deprecated `SDV_PERFORMANCE_OVERLAY` is ignored so earlier
config files no longer suppress battery display. The game alone inherits the HUD;
patcher, controller and rumble helpers run without it.

## Comparing releases

Use the same game version, save, scene, display mode and mod list. Record a second
launch after cache creation, time to title and time to load the farm. Record busy
farm frame times after warming up, plus temperature/battery behavior. Share
`log.txt` and `bash tools/diagnose-rg34xxsp` output. Build-host tests validate
behavior, not handheld speed. A shutdown-worker race is addressed, but the owner's
v0.1.3 status 135 followed deliberate Fn+Start exit. Its native cause, and earlier
status 139, remain unconfirmed. Prefer the in-game quit option for a normal
shutdown; the hotkey does not save progress.

## Runtime references

- [Microsoft: tiered compilation, quick JIT, dynamic PGO and ReadyToRun settings](https://learn.microsoft.com/en-us/dotnet/core/runtime-config/compilation)
- [Microsoft: thread-pool settings](https://learn.microsoft.com/en-us/dotnet/core/runtime-config/threading)
- [Microsoft: ReadyToRun tradeoffs](https://learn.microsoft.com/en-us/dotnet/core/deploying/ready-to-run)
