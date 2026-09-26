#!/usr/bin/env bash
# Builds what a release publishes for one runtime identifier, from a self-contained build of the launcher:
# with Velopack, the Windows installer and portable zip, the Linux AppImage or the Mac app, and their update feed;
# on Linux, also a plain archive. Velopack needs mksquashfs (squashfs-tools) to make the AppImage, and macOS to
# make the Mac app.
# Usage: mise run package [linux-x64|win-x64|osx-arm64]     (defaults to this machine's)
# Output: artifacts/packages, see packaged below; <version> is version.txt's
set -euo pipefail

root="$(cd "$(dirname "$0")/.." && pwd)"
# @spec packaging::one-version
version="$(tr -d '[:space:]' < "$root/version.txt")"

case "$(uname -s)" in MINGW* | MSYS* | CYGWIN*) machine="win-x64" ;; Darwin) machine="osx-arm64" ;; *) machine="linux-x64" ;; esac
rid="${1:-$machine}"
# @spec packaging::self-contained-archives
case "$rid" in
  linux-x64 | win-x64 | osx-arm64) ;;
  [0-9]*)
    echo "mise run package takes a runtime identifier (linux-x64, win-x64 or osx-arm64), not a version: the version is version.txt's ($version)." >&2
    exit 2
    ;;
  *)
    echo "Can't package for $rid: releases ship linux-x64, win-x64 and osx-arm64 only." >&2
    exit 2
    ;;
esac
if [ "$rid" = osx-arm64 ] && [ "$(uname -s)" != Darwin ]; then
  echo "The Mac app is signed and packed with macOS tools: package osx-arm64 on a Mac." >&2
  exit 2
fi
name="EternalfestDesktop-$version-$rid"
staging="$root/artifacts/staging/$name"
packages="$root/artifacts/packages"
velopack="$root/artifacts/velopack/$rid"
# @spec packaging::self-contained-archives
# @spec packaging::update-feed
# The Velopack id also names the Windows install folder: it must differ from the data folder, EternalfestDesktop
id="eternalfest-desktop"
case "$rid" in
  win-*) packaged="$id-win-Setup.exe $id-win-Portable.zip releases.win.json $id-$version-full.nupkg" ;;
  linux-*) packaged="$name.tar.gz $id.AppImage releases.linux.json $id-$version-linux-full.nupkg" ;;
  osx-*) packaged="$id-osx-Portable.zip releases.osx.json $id-$version-osx-full.nupkg" ;;
esac

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
  linux-*) players="ruffle/ruffle flash-player/flashplayer flash-player/lib/libprojector-window.so flash-player/lib/libgtk-x11-2.0.so.0 flash-player/lib/libnss3.so flash-player/licenses/LGPL-2.txt flash-player/licenses/MPL-2.0.txt flash-player/licenses/libgtk2.0-0.copyright flash-player/NOTICE.md" ;;
  win-*) players="ruffle/ruffle.exe flash-player/flashplayer.exe flash-player/NOTICE.md" ;;
  osx-*) players="ruffle/Ruffle.app/Contents/MacOS/ruffle" ;;
esac
for required in "EternalfestDesktop$exe" $players flash/loader.swf flash/game.swf flash/NOTICE.md ruffle/LICENSE.md LICENSE THIRD-PARTY-NOTICES.md icon.png; do
  if [ ! -f "$staging/$required" ]; then
    echo "The $rid package misses $required" >&2
    exit 1
  fi
done

rm -rf "$velopack"
mkdir -p "$velopack" "$packages"
# Before Velopack reads the build, so the plain archive holds nothing of it
case "$rid" in
  linux-*) (cd "$(dirname "$staging")" && tar -czf "$velopack/$name.tar.gz" "$name") ;;
esac

# @spec packaging::macos-app
# The app bundle is made here rather than by Velopack, which would put every file in Contents/MacOS and, to sign
# them, re-sign Ruffle's bundle too: its files go to Contents/Resources, and only the app's own code is signed.
bundle="$root/artifacts/bundle/Eternalfest Desktop.app"
make_mac_app() {
  rm -rf "$(dirname "$bundle")"
  mkdir -p "$bundle/Contents/MacOS" "$bundle/Contents/Resources"
  cp "$staging/EternalfestDesktop" "$bundle/Contents/MacOS/"
  find "$staging" -maxdepth 1 -name '*.dylib' -exec cp {} "$bundle/Contents/MacOS/" \;
  for file in "$staging"/*; do
    case "$(basename "$file")" in EternalfestDesktop | *.dylib | ruffle) ;; *) cp -R "$file" "$bundle/Contents/Resources/" ;; esac
  done
  # ditto keeps Ruffle's bundle as its authors signed it
  mkdir -p "$bundle/Contents/Resources/ruffle"
  cp "$staging/ruffle/LICENSE.md" "$bundle/Contents/Resources/ruffle/"
  ditto "$root/artifacts/ruffle/$rid/Ruffle.app" "$bundle/Contents/Resources/ruffle/Ruffle.app"

  local iconset="$root/artifacts/bundle/AppIcon.iconset"
  mkdir -p "$iconset"
  for size in 16 32 128 256 512; do
    sips -z "$size" "$size" "$staging/icon.png" --out "$iconset/icon_${size}x${size}.png" >/dev/null
    sips -z "$((size * 2))" "$((size * 2))" "$staging/icon.png" --out "$iconset/icon_${size}x${size}@2x.png" >/dev/null
  done
  iconutil --convert icns --output "$bundle/Contents/Resources/AppIcon.icns" "$iconset"

  cat > "$bundle/Contents/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleIdentifier</key><string>io.github.cmnemoi.EternalfestDesktop</string>
  <key>CFBundleName</key><string>Eternalfest Desktop</string>
  <key>CFBundleDisplayName</key><string>Eternalfest Desktop</string>
  <key>CFBundleExecutable</key><string>EternalfestDesktop</string>
  <key>CFBundleIconFile</key><string>AppIcon</string>
  <key>CFBundlePackageType</key><string>APPL</string>
  <key>CFBundleShortVersionString</key><string>$version</string>
  <key>CFBundleVersion</key><string>$version</string>
  <key>LSApplicationCategoryType</key><string>public.app-category.games</string>
  <key>NSHighResolutionCapable</key><true/>
</dict>
</plist>
PLIST
}

case "$rid" in
  win-*) pack=("[win]" --packDir "$staging" --icon "$root/src/EternalfestDesktop.Ui/Assets/icon.ico") ;;
  linux-*) pack=("[linux]" --packDir "$staging" --icon "$staging/icon.png" --categories Game) ;;
  # Signed ad hoc, not by an Apple developer; without --deep, Ruffle keeps its own signature
  osx-*) make_mac_app; pack=("[osx]" --packDir "$bundle" --signAppIdentity - --signDisableDeep --noInst) ;;
esac
(cd "$root" && dotnet tool restore >/dev/null && dotnet vpk "${pack[0]}" pack "${pack[@]:1}" \
  --packId "$id" \
  --packVersion "$version" \
  --runtime "$rid" \
  --packTitle "Eternalfest Desktop" \
  --packAuthors "Charles-Meldhine Madi Mnemoi" \
  --mainExe "EternalfestDesktop$exe" \
  --delta None \
  --outputDir "$velopack")

for file in $packaged; do
  cp "$velopack/$file" "$packages/$file" 2>/dev/null || {
    echo "The $rid release misses $file: Velopack made $(ls "$velopack")" >&2
    exit 1
  }
done

# @spec packaging::macos-app
if [ "$rid" = osx-arm64 ]; then
  unpacked="$root/artifacts/bundle/unpacked"
  rm -rf "$unpacked"
  ditto -x -k "$packages/$id-osx-Portable.zip" "$unpacked"
  app="$(find "$unpacked" -maxdepth 1 -name '*.app' | head -1)"
  codesign --verify --verbose=2 "$app"
  if ! codesign --display --verbose=2 "$app/Contents/Resources/ruffle/Ruffle.app" 2>&1 | grep -q "Authority=Developer ID Application: Ruffle LLC"; then
    echo "Ruffle lost its authors' signature in the Mac app" >&2
    exit 1
  fi
fi
echo "Packaged $packaged in $packages"
