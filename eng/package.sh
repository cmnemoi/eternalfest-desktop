#!/usr/bin/env bash
# Builds a self-contained, ready-to-run archive of the launcher for one runtime identifier.
# Usage: mise run package [linux-x64|win-x64]     (defaults to this machine's)
# Output: artifacts/packages/EternalfestDesktop-<version>-<rid>.{tar.gz,zip}, <version> being version.txt's
set -euo pipefail

root="$(cd "$(dirname "$0")/.." && pwd)"
# @spec packaging::one-version
version="$(tr -d '[:space:]' < "$root/version.txt")"

case "$(uname -s)" in MINGW* | MSYS* | CYGWIN*) machine="win-x64" ;; *) machine="linux-x64" ;; esac
rid="${1:-$machine}"
# @spec packaging::self-contained-archives
case "$rid" in
  linux-x64 | win-x64) ;;
  [0-9]*)
    echo "mise run package takes a runtime identifier (linux-x64 or win-x64), not a version: the version is version.txt's ($version)." >&2
    exit 2
    ;;
  *)
    echo "Can't package for $rid: releases ship linux-x64 and win-x64 only." >&2
    exit 2
    ;;
esac
name="EternalfestDesktop-$version-$rid"
staging="$root/artifacts/staging/$name"
packages="$root/artifacts/packages"

dotnet run "$root/eng/fetch-flash-player.cs" "$rid"
dotnet run "$root/eng/fetch-ruffle.cs" "$rid"
rm -rf "$staging"
dotnet publish "$root/src/EternalfestDesktop.Ui/EternalfestDesktop.Ui.csproj" \
  --configuration Release \
  --runtime "$rid" \
  --self-contained \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  --output "$staging"

# @spec packaging::license-notices
cp "$root/LICENSE" "$staging/LICENSE"
cp "$root/THIRD-PARTY-NOTICES.md" "$staging/THIRD-PARTY-NOTICES.md"
rm -f "$staging"/*.pdb

# @spec packaging::self-contained-archives
case "$rid" in win-*) exe=".exe" ;; *) exe="" ;; esac
case "$rid" in
  linux-*) projector="flash-player/flashplayer flash-player/lib/libprojector-window.so flash-player/lib/libgtk-x11-2.0.so.0 flash-player/lib/libnss3.so flash-player/licenses/LGPL-2.txt flash-player/licenses/MPL-2.0.txt flash-player/licenses/libgtk2.0-0.copyright" ;;
  *) projector="flash-player/flashplayer.exe" ;;
esac
for required in "EternalfestDesktop$exe" "ruffle/ruffle$exe" $projector flash-player/NOTICE.md flash/loader.swf flash/game.swf flash/NOTICE.md ruffle/LICENSE.md LICENSE THIRD-PARTY-NOTICES.md icon.png; do
  if [ ! -f "$staging/$required" ]; then
    echo "The $rid package misses $required" >&2
    exit 1
  fi
done

mkdir -p "$packages"
cd "$(dirname "$staging")"
case "$rid" in
  win-*) rm -f "$packages/$name.zip"; (cd "$name" && python3 -m zipfile -c "$packages/$name.zip" *) ;;
  *) tar -czf "$packages/$name.tar.gz" "$name" ;;
esac
echo "Packaged $(ls "$packages"/"$name".*)"
