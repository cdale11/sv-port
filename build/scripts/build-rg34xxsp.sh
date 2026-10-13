#!/bin/bash
# Retail-free cross build; use build-mainline.sh for ARM64 gameplay validation.
set -euo pipefail
ROOT="$(cd -- "$(dirname "$0")/../.." && pwd)"
MONOGAME_DIR="${MONOGAME_DIR:-"$ROOT/../MonoGame"}"
MONOGAME_DIR="$MONOGAME_DIR" bash "$ROOT/build/scripts/prepare-rg34xxsp-framework.sh"
MONOGAME_ROOT="$MONOGAME_DIR" bash "$ROOT/build/scripts/bootstrap-monogame-thirdparty.sh"
MONOGAME_DIR="$(cd "$MONOGAME_DIR" && pwd -P)"
docker build -t sdv-rg34xxsp-native -f "$ROOT/build/docker/cross-native/Dockerfile" "$ROOT/build/docker/cross-native"
docker run --rm -v "$ROOT:/workspace" sdv-rg34xxsp-native bash build/scripts/cross-native.sh
docker build -t sdv-rg34xxsp-managed -f "$ROOT/build/docker/mainline/Dockerfile" "$ROOT/build/docker/mainline"
docker run --rm -v "$ROOT:/workspace" -v "$MONOGAME_DIR:/monogame" \
  sdv-rg34xxsp-managed bash build/scripts/package-mainline.sh --inside-docker --skip-validation
echo "Built experimental ZIP; retail gameplay and hardware validation were not run."
