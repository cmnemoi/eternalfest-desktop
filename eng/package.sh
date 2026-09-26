#!/usr/bin/env bash
# Builds a self-contained, ready-to-run archive of the launcher for one runtime identifier.
# Usage: mise run package <rid>     e.g. mise run package linux-x64
# Output: artifacts/packages/EternalfestDesktop-<version>-<rid>.{tar.gz,zip}, <version> being version.txt's
set -euo pipefail

rid="$1"
root="$(cd "$(dirname "$0")/.." && pwd)"
# @spec packaging::one-version
version="$(tr -d '[:space:]' < "$root/version.txt")"
name="EternalfestDesktop-$version-$rid"
staging="$root/artifacts/staging/$name"
packages="$root/artifacts/packages"

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
cp "$root/src/EternalfestDesktop.Ui/Assets/icon.png" "$staging/icon.png"
rm -f "$staging"/*.pdb

# @spec packaging::self-contained-archives
case "$rid" in win-*) exe=".exe" ;; *) exe="" ;; esac
for required in "EternalfestDesktop$exe" "ruffle/ruffle$exe" flash/loader.swf flash/game.swf flash/NOTICE.md ruffle/LICENSE.md LICENSE THIRD-PARTY-NOTICES.md icon.png; do
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
