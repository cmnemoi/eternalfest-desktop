#!/usr/bin/env bash
# Builds a self-contained, ready-to-run archive of the launcher for one runtime identifier.
# Usage: mise run package <rid> <version>     e.g. mise run package linux-x64 0.1.0
# Output: artifacts/packages/EternalfestDesktop-<version>-<rid>.{tar.gz,zip}
set -euo pipefail

rid="$1"
version="$2"
root="$(cd "$(dirname "$0")/.." && pwd)"
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
  -p:Version="$version" \
  --output "$staging"

# @spec packaging::license-notices
cp "$root/LICENSE" "$staging/LICENSE"
cp "$root/THIRD-PARTY-NOTICES.md" "$staging/THIRD-PARTY-NOTICES.md"
rm -f "$staging"/*.pdb

# @spec packaging::self-contained-archives
case "$rid" in win-*) exe=".exe" ;; *) exe="" ;; esac
for required in "EternalfestDesktop$exe" "ruffle/ruffle$exe" flash/loader.swf flash/game.swf flash/NOTICE.md ruffle/LICENSE.md LICENSE THIRD-PARTY-NOTICES.md; do
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
