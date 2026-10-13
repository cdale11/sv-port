#!/bin/bash
set -euo pipefail
ROOT="$(cd -- "$(dirname "$0")/../.." && pwd)"
MONOGAME_DIR="${MONOGAME_DIR:-"$ROOT/../MonoGame"}"
revision=3874df98675039d368e77dca50d9a00ac970537e
if [ ! -d "$MONOGAME_DIR/.git" ]; then
  git clone --branch portmaster-stardew-mainline https://github.com/Producdevity/MonoGame.git "$MONOGAME_DIR"
  git -C "$MONOGAME_DIR" checkout "$revision"
fi
if [ "$(git -C "$MONOGAME_DIR" rev-parse HEAD)" != "$revision" ]; then
  echo "Expected MonoGame $revision. Use a separate checkout at MONOGAME_DIR." >&2
  exit 1
fi
for patch in "$ROOT/build/patches/monogame-rg34xxsp.patch" "$ROOT/build/patches/monogame-performance.patch"; do
if git -C "$MONOGAME_DIR" apply --check "$patch" 2>/dev/null; then
  git -C "$MONOGAME_DIR" apply "$patch"
elif ! git -C "$MONOGAME_DIR" apply --reverse --check "$patch" 2>/dev/null; then
  echo "MonoGame contains conflicting local changes; refusing to overwrite them." >&2
  exit 1
fi
done
# SDK projects include root .cs files automatically.
cp "$ROOT/build/rg34xxsp/PortMasterRumble.cs" "$MONOGAME_DIR/MonoGame.Framework/PortMasterRumble.cs"
cp "$ROOT/build/rg34xxsp/PortMasterRuntime.cs" "$MONOGAME_DIR/MonoGame.Framework/PortMasterRuntime.cs"
