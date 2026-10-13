#!/bin/bash
set -euo pipefail
ROOT="$(cd -- "$(dirname "$0")/../.." && pwd)"
OUT="$ROOT/build/out/mainline/native-shims"
CC="${CC:-aarch64-linux-gnu-gcc}"
mkdir -p "$OUT"
for spec in 'imm32_shim.c:Imm32.dll' 'user32_shim.c:user32.dll' \
  'sdl2_clipboard_shim.c:SDL2.dll' 'steam_api64_stub.c:libsteam_api64.so' \
  'sdkencryptedappticket64_stub.c:libsdkencryptedappticket64.so'; do
  "$CC" -shared -fPIC -O2 -s "$ROOT/build/src/native/${spec%%:*}" -o "$OUT/${spec#*:}" -ldl
done
cp "$OUT/Imm32.dll" "$OUT/libImm32.dll.so"
cp "$OUT/user32.dll" "$OUT/libuser32.dll.so"
cp "$OUT/SDL2.dll" "$OUT/libSDL2.dll.so"
"$CC" -std=c11 -O2 -Wall -Wextra -Werror -s "$ROOT/build/src/native/rg34xxsp_rumble.c" -o "$OUT/rg34xxsp-rumble"
for binary in "$OUT"/*; do
  file "$binary" | grep -q 'ARM aarch64' || { echo "Wrong architecture: $binary"; exit 1; }
  if aarch64-linux-gnu-strings "$binary" | grep -Eq 'GLIBC_2\.(3[4-9]|[4-9][0-9])'; then
    echo "Too-new glibc in $binary; build with the Debian 11 cross image." >&2
    exit 1
  fi
done
