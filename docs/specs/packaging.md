# Packaging v1

## Why

"Plug and play" means a player downloads one archive, extracts it and double-clicks. Nothing else to install.

## Scope

The first release format: self-contained archives for Windows x64 and Linux x64, published on GitHub Releases. Auto-update, installers, macOS and stores come later.

## Rules

### Self-contained archives

`{#packaging::self-contained-archives}`

Each release publishes `EternalfestDesktop-{version}-win-x64.zip` and `EternalfestDesktop-{version}-linux-x64.tar.gz`. Each contains the launcher (with the .NET runtime), the pinned Flash projector for that OS when there is one (Linux, with its GTK 2 and NSS libraries and `libprojector-window.so`), the pinned Ruffle binary for that OS, the bundled loader and base engine, the app icon as `icon.png` (for a Linux shortcut), and the license notices. No other installation is needed.

### License notices

`{#packaging::license-notices}`

Archives ship the app's GPL-3.0 license, a notice for the Flash projector and its libraries (`flash-player/NOTICE.md`), with Adobe's license and the LGPL notices it ships with, and, on Linux, the LGPL-2.0 and MPL-2.0 texts and the Debian copyright files of GTK 2, NSS and NSPR, with links to their sources, Ruffle's MIT/Apache-2.0 notices, the loader's AGPL-3.0 notice with a link to its sources, and the base engine's MIT notice.

### Released by merging the release pull request

`{#packaging::released-from-release-pr}`

Once the integration checks pass on `main`, release-please keeps one pull request open with the next version and its changelog, computed from the conventional commits since the last release. Merging it tags `v{version}`, creates the GitHub Release, and attaches both archives. Nothing is released without that merge, and a failing check on `main` releases nothing.

Versions follow semantic versioning from `0.1.0`: a `fix` bumps the patch, a `feat` the minor, and a breaking change (`feat!`, `BREAKING CHANGE`) the major, even before `1.0.0`.

### One version everywhere

`{#packaging::one-version}`

`version.txt`, maintained by release-please, is the only place the version is written. The app, its logs, its archives and the release tag all carry it.

### Data outside the install folder

`{#packaging::data-outside-install}`

The app never writes inside its own folder. Cache, catalog, settings and logs go to `%LOCALAPPDATA%\EternalfestDesktop` on Windows, and `$XDG_DATA_HOME/eternalfest-desktop` (default `~/.local/share/eternalfest-desktop`) on Linux, unless another cache folder is chosen in settings.

### Linux applications menu

`{#packaging::linux-desktop-entry}`

On Linux, the app adds itself to the user's applications menu each time it starts: a desktop entry in `$XDG_DATA_HOME/applications` (default `~/.local/share/applications`) that runs the app from where it is, and its icon in the user's `hicolor` icon theme. The entry is only rewritten when it changed, so moving the extracted folder moves the entry with it. An entry whose app was deleted is hidden by the desktop, since it names the app as `TryExec`. The window's class matches the entry, so docks and task switchers show the app's icon and name. A failure to write the entry is logged and never prevents the app from starting. Development builds leave the menu alone.

## Acceptance criteria

- Given a clean Windows 11 x64 machine, when the zip is extracted and the executable double-clicked, then the library opens and a contrée can be played.
- Given a clean Ubuntu LTS desktop, when the tar.gz is extracted and the executable launched, then the library opens and a contrée can be played.
- Given the app is extracted in a read-only folder, when it runs, then it works.
- Given the Linux app extracted in `/home/player/Jeux/Eternalfest Desktop`, when it starts, then the applications menu has an "Eternalfest Desktop" entry with the app's icon, which starts that app.
- Given the entry is already up to date, when the app starts, then the entry file isn't written again. Given the folder was moved, then the entry points to the new place.
- Given the applications folder can't be written, when the app starts, then it starts anyway and a warning is logged.
- Given the release pull request is merged, when the delivery runs, then both archives are attached to the GitHub Release with their license notices.
- Given `version.txt` holds `0.2.0`, when the app is built, then its assembly version is `0.2.0`.

## Out of scope

- Code signing (Windows SmartScreen will warn on first launch, and the README explains it).
- Auto-update (Velopack), AppImage, Flatpak, winget and macOS: later lots.
