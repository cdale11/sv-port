#!/bin/bash
set -euo pipefail
ROOT="$(cd -- "$(dirname "$0")/.." && pwd)"
DEST="${1:?Usage: stage-package-docs.sh <package-root>}"
mkdir -p "$DEST/docs"
cp "$ROOT/README.md" "$ROOT/CHANGELOG.md" "$ROOT/LICENSE" "$DEST/"
cp "$ROOT/docs/"*.md "$DEST/docs/"
