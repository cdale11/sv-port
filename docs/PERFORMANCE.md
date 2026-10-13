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

Preparation caching skips repeat patching when the inputs are unchanged. Game
construction and initialization can still take tens of seconds. Startup logs
separate launcher preparation, managed startup and framework stages, with CPU
count, affinity and entry assembly identity. A cache hit does not imply that
retail game content and initialization are cached.

Tiered compilation first runs quick code, then recompiles hot code in the
background. Dynamic PGO uses runtime profiles to optimize that hot code. Quick
loop compilation can shorten initial compilation but can run slower until a
method is promoted. Defaults enable all three; compare the same save after a
warm-up, not only its first frame. Keep the shipped defaults for normal play.
Optional diagnostic comparisons in `rg34xxsp.conf`:

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
behavior; actual speed depends on storage, saves, game version and configuration.
Prefer the in-game quit option for normal shutdown. Fn+Start does not save
progress and can produce a native exit error; see [known limitations](HARDWARE-VALIDATION.md).

## Runtime references

- [Microsoft: tiered compilation, quick JIT, dynamic PGO and ReadyToRun settings](https://learn.microsoft.com/en-us/dotnet/core/runtime-config/compilation)
- [Microsoft: thread-pool settings](https://learn.microsoft.com/en-us/dotnet/core/runtime-config/threading)
- [Microsoft: ReadyToRun tradeoffs](https://learn.microsoft.com/en-us/dotnet/core/deploying/ready-to-run)
