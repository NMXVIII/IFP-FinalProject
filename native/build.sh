#!/usr/bin/env bash
# Builds the native library for the CURRENT macOS/Linux machine.
# Run from anywhere: bash native/build.sh
set -euo pipefail
cd "$(dirname "$0")/.."
os="$(uname -s)"; arch="$(uname -m)"
case "$os-$arch" in
  Darwin-arm64)  rid=osx-arm64;  out=libboardgame_native.dylib; flags="-dynamiclib -arch arm64" ;;
  Darwin-x86_64) rid=osx-x64;    out=libboardgame_native.dylib; flags="-dynamiclib -arch x86_64" ;;
  Linux-x86_64)  rid=linux-x64;  out=libboardgame_native.so;    flags="-shared -fPIC" ;;
  *) echo "Unsupported platform $os-$arch (use native/build.ps1 on Windows x64)." >&2; exit 1 ;;
esac
mkdir -p "runtimes/$rid/native"
cc="${CC:-clang}"; command -v "$cc" >/dev/null || cc=gcc
"$cc" -std=c11 -O2 -Wall -Wextra -Werror $flags -o "runtimes/$rid/native/$out" native/boardgame_native.c
echo "Built runtimes/$rid/native/$out with $("$cc" --version | head -n1)"
