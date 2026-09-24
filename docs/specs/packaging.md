# Packaging v1

## Why

"Plug and play" means a player downloads one archive, extracts it and double-clicks. Nothing else to install.

## Scope

The first release format: self-contained archives for Windows x64 and Linux x64, published on GitHub Releases. Auto-update, installers, macOS and stores come later.

## Rules

### Self-contained archives

`{#packaging::self-contained-archives}`

Each release publishes `EternalfestDesktop-{version}-win-x64.zip` and `EternalfestDesktop-{version}-linux-x64.tar.gz`. Each contains the launcher (with the .NET runtime), the pinned Ruffle binary for that OS, the bundled loader and base engine, and the license notices. No other installation is needed.

### License notices

`{#packaging::license-notices}`

Archives ship the app's GPL-3.0 license, Ruffle's MIT/Apache-2.0 notices, the loader's AGPL-3.0 notice with a link to its sources, and the base engine's MIT notice.

### Released from a tag

`{#packaging::released-from-tag}`

Pushing a `v{version}` tag builds, tests and publishes both archives to a GitHub Release. A failing test prevents the release.

### Data outside the install folder

`{#packaging::data-outside-install}`

The app never writes inside its own folder. Cache, catalog, settings and logs go to `%LOCALAPPDATA%\EternalfestDesktop` on Windows, and `$XDG_DATA_HOME/eternalfest-desktop` (default `~/.local/share/eternalfest-desktop`) on Linux, unless another cache folder is chosen in settings.

## Acceptance criteria

- Given a clean Windows 11 x64 machine, when the zip is extracted and the executable double-clicked, then the library opens and a contrée can be played.
- Given a clean Ubuntu LTS desktop, when the tar.gz is extracted and the executable launched, then the library opens and a contrée can be played.
- Given the app is extracted in a read-only folder, when it runs, then it works.
- Given a release tag, when CI runs, then both archives are attached to the GitHub Release with their license notices.

## Out of scope

- Code signing (Windows SmartScreen will warn on first launch, and the README explains it).
- Auto-update (Velopack), AppImage, Flatpak, winget and macOS: later lots.
