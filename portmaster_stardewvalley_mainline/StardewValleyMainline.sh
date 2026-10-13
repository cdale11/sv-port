#!/bin/bash
startup_seconds=$SECONDS

XDG_DATA_HOME=${XDG_DATA_HOME:-$HOME/.local/share}

if [ -d "/opt/system/Tools/PortMaster/" ]; then
  controlfolder="/opt/system/Tools/PortMaster"
elif [ -d "/opt/tools/PortMaster/" ]; then
  controlfolder="/opt/tools/PortMaster"
elif [ -d "$XDG_DATA_HOME/PortMaster/" ]; then
  controlfolder="$XDG_DATA_HOME/PortMaster"
else
  controlfolder="/roms/ports/PortMaster"
fi

if [ ! -r "$controlfolder/control.txt" ]; then
  echo "PortMaster is missing. Install/update PortMaster in KNULLI first." >&2
  exit 1
fi
source "$controlfolder/control.txt"
[ -f "${controlfolder}/mod_${CFW_NAME}.txt" ] && source "${controlfolder}/mod_${CFW_NAME}.txt"
get_controls

[ -n "${DISPLAY_WIDTH:-}" ] && export DISPLAY_WIDTH
[ -n "${DISPLAY_HEIGHT:-}" ] && export DISPLAY_HEIGHT

GAMEDIR="${SDV_GAMEDIR:-"/$directory/ports/stardewvalleymainline"}"
gamedir="$GAMEDIR"

cd "$GAMEDIR" || exit 1
# Prevent two patchers/games sharing the same mutable game files and motor.
exec 9>"$GAMEDIR/.launch.lock"
if ! flock -n 9; then
  echo "Stardew Valley is already running." >&2
  exit 1
fi
> "$GAMEDIR/log.txt" && exec > >(tee "$GAMEDIR/log.txt") 2>&1

if [ -f "$gamedir/rg34xxsp.conf" ]; then source "$gamedir/rg34xxsp.conf"; fi
source "$gamedir/tools/rg34xxsp-profile"
source "$gamedir/tools/cpu-performance"
sdv_governor_paths=()
sdv_governor_original=()
if [ "$(uname -m)" != aarch64 ]; then
  echo "This port requires 64-bit KNULLI (aarch64)."
  exit 1
fi
game_pid=
gptokeyb_pid=
cleanup() {
  trap - EXIT INT TERM HUP
  if [ -n "$game_pid" ]; then kill "$game_pid" 2>/dev/null || true; wait "$game_pid" 2>/dev/null || true; fi
  if [ -n "$gptokeyb_pid" ]; then kill "$gptokeyb_pid" 2>/dev/null || true; wait "$gptokeyb_pid" 2>/dev/null || true; fi
  if [ -n "${SDV_RUMBLE_HELPER:-}" ]; then printf '0\n' > "$SDV_RUMBLE_DEVICE" || true; fi
  sdv_cpu_end
  if command -v pm_finish >/dev/null 2>&1; then pm_finish; fi
}
trap cleanup EXIT
trap 'exit 130' INT
trap 'exit 143' TERM
trap 'exit 129' HUP

CUR_TTY="${CUR_TTY:-/dev/tty0}"
if [ "$CUR_TTY" != /dev/null ]; then $ESUDO chmod 666 "$CUR_TTY"; fi
printf "\033c" > "$CUR_TTY"
echo "Loading... Please Wait." > "$CUR_TTY"

mkdir -p "$gamedir/savedata"
bind_directories "${XDG_CONFIG_HOME:-$HOME/.config}/StardewValley" "$gamedir/savedata"

modsdir="$gamedir/Mods"
smapi_bundle_dir="$gamedir/tools/SMAPIBundle"
smapi_game_root="$smapi_bundle_dir/game-root"
smapi_default_mods_dir="$smapi_bundle_dir/default-mods"
smapi_helper="$gamedir/tools/smapi-common"

if [ ! -f "$smapi_helper" ]; then
  pm_message "Missing SMAPI helper script at $smapi_helper."
  sleep 5
  exit 1
fi

source "$smapi_helper"
mkdir -p "$modsdir"

launch_mode="$(sdv_smapi_determine_mode "$modsdir")"
case "${SDV_LAUNCH_MODE:-auto}" in
  auto) ;;
  vanilla|smapi) launch_mode="$SDV_LAUNCH_MODE" ;;
  *) echo "SDV_LAUNCH_MODE must be auto, vanilla or smapi."; exit 1 ;;
esac
entry_assembly="Stardew Valley.dll"
smapi_patch_path="$gamedir/dlls/StardewPatches.dll"
patcher_args=(
  --game-dir "$gamedir/gamedata"
  --overlay-dir "$gamedir/overrides/gamedata"
)

if [ "$launch_mode" = "smapi" ]; then
  entry_assembly="StardewModdingAPI.dll"
  smapi_patch_path="$gamedir/gamedata/smapi-internal/StardewPatches.dll"
  patcher_args+=(
    --mods-dir "$modsdir"
    --smapi-bundle-dir "$smapi_game_root"
    --smapi-patch-assembly "$gamedir/dlls/StardewPatches.dll"
  )
fi

export DOTNET_ROOT="$gamedir/dotnet"
if [ ! -x "$DOTNET_ROOT/dotnet" ]; then
  pm_message "Missing bundled ARM64 .NET runtime. Install the complete release ZIP."
  exit 1
fi
if [ -f "$gamedir/profiling.enabled" ]; then
  mkdir -p "$gamedir/logs"
  rm -f \
    "$gamedir/logs/perf-latest.csv" \
    "$gamedir/logs/content-load-latest.csv" \
    "$gamedir/logs/slow-frame-latest.csv"
  export SDV_PERF_LOG="$gamedir/logs/perf-latest.csv"
  export SDV_CONTENT_PROFILE_LOG="$gamedir/logs/content-load-latest.csv"
  export SDV_SLOW_FRAME_LOG="$gamedir/logs/slow-frame-latest.csv"
else
  unset SDV_PERF_LOG
  unset SDV_CONTENT_PROFILE_LOG
  unset SDV_SLOW_FRAME_LOG
fi
port_arch="${DEVICE_ARCH:-aarch64}"
export LD_LIBRARY_PATH="$gamedir/libs.${port_arch}${LD_LIBRARY_PATH:+:$LD_LIBRARY_PATH}"
export SDL_GAMECONTROLLERCONFIG="$sdl_controllerconfig"

# Request libGL from PortMaster.
if [ -f "${controlfolder}/libgl_${CFW_NAME}.txt" ]; then
  source "${controlfolder}/libgl_${CFW_NAME}.txt"
else
  source "${controlfolder}/libgl_default.txt"
fi

if [[ "$LIBGL_ES" != "" ]]; then
  export SDL_VIDEO_GL_DRIVER="${gamedir}/gl4es.${port_arch}/libGL.so.1"
  export SDL_VIDEO_EGL_DRIVER="${gamedir}/gl4es.${port_arch}/libEGL.so.1"
fi

if [ ! -f "$gamedir/gamedata/Stardew Valley.dll" ]; then
  if [ -f "$gamedir/gamedata/Stardew Valley.exe" ] || [ -f "$gamedir/gamedata/StardewValley.exe" ]; then
    pm_message "Wrong Stardew Valley files. This port needs the Windows or Linux Steam mainline version, not the compatibility branch. Switch Steam out of the compatibility beta and copy the game files again."
  else
    pm_message "Missing Stardew Valley game files. Copy the Windows or Linux Steam mainline version into ports/stardewvalleymainline/gamedata."
  fi
  sleep 5
  exit 1
fi

if [ ! -f "$gamedir/gamedata/Stardew Valley.deps.json" ] || [ ! -f "$gamedir/gamedata/Stardew Valley.runtimeconfig.json" ]; then
  pm_message "Incomplete Stardew Valley files. This port needs the Windows or Linux Steam mainline build, not the compatibility branch."
  sleep 5
  exit 1
fi

source "$gamedir/tools/preparation-cache"
prepare_seconds=$SECONDS
if sdv_preparation_is_current; then
  echo "Preparation cache hit; unchanged game files will not be repatched."
else
  rm -f "$gamedir/.preparation-cache"
  if [ "$launch_mode" = smapi ]; then
    sdv_smapi_sync_default_mods "$smapi_default_mods_dir" "$modsdir"
  fi
  if ! env LD_PRELOAD="$SDV_NONHUD_PRELOAD" MANGOHUD=0 DISABLE_MANGOHUD=1 "$DOTNET_ROOT/dotnet" "$gamedir/tools/MainlineGameDataPatcher/MainlineGameDataPatcher.dll" "${patcher_args[@]}"; then
    cat "${gamedir}/log.txt" > "$CUR_TTY"
    sleep 5
    exit 1
  fi
  sdv_preparation_store || echo "Preparation cache unavailable; next launch will prepare normally."
fi
echo "Startup preparation: $((SECONDS - prepare_seconds))s; launcher elapsed: $((SECONDS - startup_seconds))s."

export MONOGAME_PATCH="$smapi_patch_path"
if [ "$launch_mode" = "smapi" ]; then
  export SMAPI_MODS_PATH="$modsdir"
  export SMAPI_USE_CURRENT_SHELL=true
else
  unset SMAPI_MODS_PATH
  unset SMAPI_USE_CURRENT_SHELL
fi

cd "$gamedir/gamedata"

env LD_PRELOAD="$SDV_NONHUD_PRELOAD" MANGOHUD=0 DISABLE_MANGOHUD=1 $GPTOKEYB "$DOTNET_ROOT/dotnet" &
gptokeyb_pid=$!
command -v pm_platform_helper >/dev/null 2>&1 && pm_platform_helper "$DOTNET_ROOT/dotnet"
printf 'Starting %s at %(%Y-%m-%dT%H:%M:%S%z)T; launcher elapsed: %ss.\n' "$entry_assembly" -1 "$((SECONDS - startup_seconds))"
sdv_cpu_begin
# Let .NET see the actual affinity, rather than an inherited manual count cap.
unset DOTNET_PROCESSOR_COUNT
export DOTNET_STARTUP_HOOKS="$gamedir/dlls/StardewPatches.dll${DOTNET_STARTUP_HOOKS:+:$DOTNET_STARTUP_HOOKS}"
"${sdv_cpu_args[@]}" "$DOTNET_ROOT/dotnet" "$entry_assembly" &
game_pid=$!
wait "$game_pid"
game_status=$?
game_pid=
echo "Stardew Valley exited with status ${game_status}."
if [ "$game_status" = 139 ]; then
  echo "Native process ended with SIGSEGV; note whether it happened during play or after quitting. CPU tuning does not diagnose the native fault."
fi
exit "$game_status"
